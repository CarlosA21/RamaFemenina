using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace RamaFemenina.Services
{
    /// <summary>
    /// Servicio para gestionar la secuencia autoincremental de números NCF
    /// </summary>
    public class NcfSequenceService
    {
        private readonly string _sequenceFilePath;
        private NcfSequenceConfig _config;

        public class NcfSequenceConfig
        {
            public int NumeroActual { get; set; }
            public int NumeroInicio { get; set; }
            public int NumeroFin { get; set; }
            public bool SecuenciaActiva { get; set; }
        }

        public NcfSequenceService()
        {
            // Guardar el archivo de configuración en AppData local
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(appDataPath, "RamaFemenina");
            Directory.CreateDirectory(appFolder);
            _sequenceFilePath = Path.Combine(appFolder, "ncf_sequence.json");
            
            CargarConfiguracion();
        }

        /// <summary>
        /// Carga la configuración desde el archivo o crea una nueva
        /// </summary>
        private void CargarConfiguracion()
        {
            try
            {
                if (File.Exists(_sequenceFilePath))
                {
                    var json = File.ReadAllText(_sequenceFilePath);
                    _config = JsonSerializer.Deserialize<NcfSequenceConfig>(json) ?? CrearConfiguracionPorDefecto();
                }
                else
                {
                    _config = CrearConfiguracionPorDefecto();
                    GuardarConfiguracion();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NCF-SEQUENCE] Error cargando configuración: {ex.Message}");
                _config = CrearConfiguracionPorDefecto();
            }
        }

        /// <summary>
        /// Guarda la configuración en el archivo
        /// </summary>
        private void GuardarConfiguracion()
        {
            try
            {
                var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_sequenceFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NCF-SEQUENCE] Error guardando configuración: {ex.Message}");
            }
        }

        /// <summary>
        /// Crea una configuración por defecto
        /// </summary>
        private NcfSequenceConfig CrearConfiguracionPorDefecto()
        {
            return new NcfSequenceConfig
            {
                NumeroActual = 500,
                NumeroInicio = 500,
                NumeroFin = 1000,
                SecuenciaActiva = false
            };
        }

        /// <summary>
        /// Configura una nueva secuencia
        /// </summary>
        /// <param name="inicio">Número de inicio de la secuencia</param>
        /// <param name="fin">Número final de la secuencia</param>
        public void ConfigurarSecuencia(int inicio, int fin)
        {
            if (inicio <= 0)
                throw new ArgumentException("El número de inicio debe ser mayor a 0");
            
            if (fin <= inicio)
                throw new ArgumentException("El número final debe ser mayor al número de inicio");

            _config.NumeroInicio = inicio;
            _config.NumeroFin = fin;
            _config.NumeroActual = inicio;
            _config.SecuenciaActiva = true;
            
            GuardarConfiguracion();
        }

        /// <summary>
        /// Obtiene el siguiente número de la secuencia
        /// </summary>
        /// <returns>El siguiente número, o null si la secuencia está inactiva o se ha agotado</returns>
        public int? ObtenerSiguienteNumero()
        {
            if (!_config.SecuenciaActiva)
                return null;

            if (_config.NumeroActual > _config.NumeroFin)
            {
                _config.SecuenciaActiva = false;
                GuardarConfiguracion();
                return null;
            }

            var numeroActual = _config.NumeroActual;
            _config.NumeroActual++;
            GuardarConfiguracion();
            
            return numeroActual;
        }

        /// <summary>
        /// Obtiene el número actual sin incrementarlo
        /// </summary>
        public int? ObtenerNumeroActual()
        {
            if (!_config.SecuenciaActiva)
                return null;

            if (_config.NumeroActual > _config.NumeroFin)
                return null;

            return _config.NumeroActual;
        }

        /// <summary>
        /// Verifica si la secuencia está activa
        /// </summary>
        public bool EstaActiva()
        {
            return _config.SecuenciaActiva && _config.NumeroActual <= _config.NumeroFin;
        }

        /// <summary>
        /// Obtiene información sobre el estado de la secuencia
        /// </summary>
        public (bool activa, int actual, int inicio, int fin, int restantes) ObtenerEstado()
        {
            var restantes = _config.SecuenciaActiva ? Math.Max(0, _config.NumeroFin - _config.NumeroActual + 1) : 0;
            return (
                _config.SecuenciaActiva && _config.NumeroActual <= _config.NumeroFin,
                _config.NumeroActual,
                _config.NumeroInicio,
                _config.NumeroFin,
                restantes
            );
        }

        /// <summary>
        /// Desactiva la secuencia actual
        /// </summary>
        public void DesactivarSecuencia()
        {
            _config.SecuenciaActiva = false;
            GuardarConfiguracion();
        }

        /// <summary>
        /// Reinicia la secuencia al número de inicio
        /// </summary>
        public void ReiniciarSecuencia()
        {
            _config.NumeroActual = _config.NumeroInicio;
            _config.SecuenciaActiva = true;
            GuardarConfiguracion();
        }
    }
}
