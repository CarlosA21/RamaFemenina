using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using RamaFemenina.Services;

namespace RamaFemenina.Common;

/// <summary>
/// Clase base para páginas con navegación optimizada y sin conflictos de threading
/// </summary>
public abstract class BaseNavigationPage : Page, INotifyPropertyChanged, IDisposable
{
    protected readonly DataCacheService CacheService;
    protected readonly IServiceProvider ServiceProvider;
    
    private bool _isPageActive;
    private bool _isInitialized;
    private bool _disposed;
    private CancellationTokenSource _pageCancellationTokenSource;

    /// <summary>
    /// Indica si la página está activa y visible
    /// </summary>
    protected bool IsPageActive
    {
        get => _isPageActive;
        private set
        {
            if (_isPageActive != value)
            {
                _isPageActive = value;
                OnPageActiveStateChanged(value);
            }
        }
    }

    /// <summary>
    /// Indica si la página ya fue inicializada
    /// </summary>
    protected bool IsInitialized => _isInitialized;

    /// <summary>
    /// Token de cancelación para operaciones de la página
    /// </summary>
    protected CancellationToken PageCancellationToken => _pageCancellationTokenSource?.Token ?? CancellationToken.None;

    public event PropertyChangedEventHandler PropertyChanged;

    protected BaseNavigationPage()
    {
        var app = Application.Current as App;
        ServiceProvider = app!.Services;
        CacheService = ServiceProvider.GetService(typeof(DataCacheService)) as DataCacheService;
        
        NavigationCacheMode = NavigationCacheMode.Enabled;
        
        // Crear token de cancelación inicial
        _pageCancellationTokenSource = new CancellationTokenSource();
    }

    #region Navigation Events

    protected sealed override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        IsPageActive = true;
        
        // Cancelar operaciones anteriores
        await CancelPreviousOperationsAsync();
        
        // Crear nuevo token de cancelación
        _pageCancellationTokenSource?.Dispose();
        _pageCancellationTokenSource = new CancellationTokenSource();
        
        // Inicialización única
        if (!_isInitialized)
        {
            await InitializePageAsync();
            _isInitialized = true;
        }
        
        // Navegación
        await HandleNavigationAsync(e);
    }

    protected sealed override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        IsPageActive = false;
        
        // Cancelar operaciones en curso pero no disponer recursos
        _pageCancellationTokenSource?.Cancel();
    }

    #endregion

    #region Abstract/Virtual Methods

    /// <summary>
    /// Inicialización única de la página (solo se ejecuta una vez)
    /// </summary>
    protected virtual async Task InitializePageAsync()
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Maneja la navegación hacia esta página
    /// </summary>
    protected virtual async Task HandleNavigationAsync(NavigationEventArgs e)
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Se llama cuando cambia el estado activo de la página
    /// </summary>
    protected virtual void OnPageActiveStateChanged(bool isActive)
    {
        // Override en clases derivadas si es necesario
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Ejecuta una operación de forma segura en el UI thread
    /// </summary>
    protected async Task ExecuteOnUIThreadAsync(Action action)
    {
        if (_disposed || !IsPageActive) return;

        if (DispatcherQueue.HasThreadAccess)
        {
            action();
        }
        else
        {
            var tcs = new TaskCompletionSource<bool>();
            
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
            {
                try
                {
                    if (!_disposed && IsPageActive)
                    {
                        action();
                    }
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            
            await tcs.Task;
        }
    }

    /// <summary>
    /// Ejecuta una operación async de forma segura en el UI thread
    /// </summary>
    protected async Task ExecuteOnUIThreadAsync(Func<Task> asyncAction)
    {
        if (_disposed || !IsPageActive) return;

        if (DispatcherQueue.HasThreadAccess)
        {
            await asyncAction();
        }
        else
        {
            var tcs = new TaskCompletionSource<bool>();
            
            DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, async () =>
            {
                try
                {
                    if (!_disposed && IsPageActive)
                    {
                        await asyncAction();
                    }
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            
            await tcs.Task;
        }
    }

    /// <summary>
    /// Cancela operaciones anteriores de forma segura
    /// </summary>
    private async Task CancelPreviousOperationsAsync()
    {
        try
        {
            if (_pageCancellationTokenSource != null && !_pageCancellationTokenSource.Token.IsCancellationRequested)
            {
                _pageCancellationTokenSource.Cancel();
                
                // Dar tiempo para que las operaciones se cancelen
                await Task.Delay(100);
            }
        }
        catch (ObjectDisposedException)
        {
            // Ignorar si ya fue disposed
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error cancelando operaciones: {ex.Message}");
        }
    }

    /// <summary>
    /// Muestra un diálogo de información de forma segura
    /// </summary>
    protected async Task ShowInfoDialogAsync(string title, string message)
    {
        if (_disposed || !IsPageActive) return;

        await ExecuteOnUIThreadAsync(async () =>
        {
            if (XamlRoot == null) return;

            try
            {
                var dialog = new ContentDialog
                {
                    Title = title,
                    Content = new TextBlock 
                    { 
                        Text = message, 
                        TextWrapping = TextWrapping.Wrap,
                        MaxWidth = 400
                    },
                    CloseButtonText = "Aceptar",
                    XamlRoot = XamlRoot
                };

                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error mostrando diálogo: {ex.Message}");
            }
        });
    }

    #endregion

    #region INotifyPropertyChanged

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        if (_disposed) return;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    #region IDisposable

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                try
                {
                    _pageCancellationTokenSource?.Cancel();
                    _pageCancellationTokenSource?.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error en dispose: {ex.Message}");
                }
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    ~BaseNavigationPage()
    {
        Dispose(disposing: false);
    }

    #endregion
}