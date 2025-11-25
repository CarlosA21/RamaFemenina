using System;
using System.Globalization;

namespace RamaFemenina.Utils
{
    public static class NumeroALetras
    {
        private static readonly string[] unidades = { "", "uno", "dos", "tres", "cuatro", "cinco", "seis", "siete", "ocho", "nueve" };
        private static readonly string[] decenas = { "", "", "veinte", "treinta", "cuarenta", "cincuenta", "sesenta", "setenta", "ochenta", "noventa" };
        private static readonly string[] centenas = { "", "ciento", "doscientos", "trescientos", "cuatrocientos", "quinientos", "seiscientos", "setecientos", "ochocientos", "novecientos" };
        private static readonly string[] especiales = { "diez", "once", "doce", "trece", "catorce", "quince", "dieciséis", "diecisiete", "dieciocho", "diecinueve" };

        public static string ConvertirNumeroALetras(decimal numero)
        {
            if (numero == 0)
                return "cero pesos dominicanos";

            long parteEntera = (long)numero;
            int centavos = (int)((numero - parteEntera) * 100);

            string resultado = ConvertirEnteroALetras(parteEntera);
            
            if (parteEntera == 1)
                resultado += " peso dominicano";
            else
                resultado += " pesos dominicanos";

            if (centavos > 0)
            {
                resultado += " con " + ConvertirEnteroALetras(centavos);
                if (centavos == 1)
                    resultado += " centavo";
                else
                    resultado += " centavos";
            }

            return resultado.Trim();
        }

        private static string ConvertirEnteroALetras(long numero)
        {
            if (numero == 0)
                return "";

            if (numero < 0)
                return "menos " + ConvertirEnteroALetras(-numero);

            string resultado = "";

            // Millones
            if (numero >= 1000000)
            {
                long millones = numero / 1000000;
                if (millones == 1)
                    resultado += "un millón ";
                else
                    resultado += ConvertirEnteroALetras(millones) + " millones ";
                numero %= 1000000;
            }

            // Miles
            if (numero >= 1000)
            {
                long miles = numero / 1000;
                if (miles == 1)
                    resultado += "mil ";
                else
                    resultado += ConvertirEnteroALetras(miles) + " mil ";
                numero %= 1000;
            }

            // Centenas, decenas y unidades
            if (numero > 0)
            {
                resultado += ConvertirCientoALetras((int)numero);
            }

            return resultado.Trim();
        }

        private static string ConvertirCientoALetras(int numero)
        {
            if (numero == 0)
                return "";

            string resultado = "";

            // Centenas
            if (numero >= 100)
            {
                int centena = numero / 100;
                if (numero == 100)
                    resultado += "cien";
                else
                    resultado += centenas[centena];
                numero %= 100;
                
                if (numero > 0)
                    resultado += " ";
            }

            // Decenas y unidades
            if (numero >= 20)
            {
                int decena = numero / 10;
                resultado += decenas[decena];
                numero %= 10;
                
                if (numero > 0)
                {
                    resultado += " y " + unidades[numero];
                }
            }
            else if (numero >= 10)
            {
                resultado += especiales[numero - 10];
            }
            else if (numero > 0)
            {
                resultado += unidades[numero];
            }

            return resultado;
        }

        public static string ConvertirMonedaALetras(decimal monto)
        {
            string letras = ConvertirNumeroALetras(monto);
            return char.ToUpper(letras[0]) + letras.Substring(1);
        }
    }
}