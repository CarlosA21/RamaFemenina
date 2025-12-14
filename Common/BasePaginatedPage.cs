using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Navigation;

namespace RamaFemenina.Common;

/// <summary>
/// Clase base para páginas con funcionalidad de paginación
/// </summary>
public abstract class BasePaginatedPage<T> : BaseNavigationPage where T : class
{
    private bool _isLoading;
    private int _currentPage = 1;
    private int _pageSize = 50;
    private int _totalCount = 0;
    private string _searchTerm = "";
    
    // Colección thread-safe para los datos
    public ObservableCollection<T> Items { get; private set; }

    #region Properties

    public bool IsLoading
    {
        get => _isLoading;
        protected set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnPropertyChanged();
                OnLoadingStateChanged(value);
            }
        }
    }

    public int CurrentPage
    {
        get => _currentPage;
        protected set
        {
            if (_currentPage != value)
            {
                _currentPage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalPages));
                OnPropertyChanged(nameof(HasPreviousPage));
                OnPropertyChanged(nameof(HasNextPage));
                OnPropertyChanged(nameof(PageInfo));
            }
        }
    }

    public int PageSize
    {
        get => _pageSize;
        protected set
        {
            if (_pageSize != value)
            {
                _pageSize = value;
                OnPropertyChanged();
            }
        }
    }

    public int TotalCount
    {
        get => _totalCount;
        protected set
        {
            if (_totalCount != value)
            {
                _totalCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalPages));
                OnPropertyChanged(nameof(PageInfo));
            }
        }
    }

    public string SearchTerm
    {
        get => _searchTerm;
        protected set
        {
            if (_searchTerm != value)
            {
                _searchTerm = value;
                OnPropertyChanged();
            }
        }
    }

    // Propiedades calculadas
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    public string PageInfo => $"Página {CurrentPage} de {TotalPages} ({TotalCount} registros)";

    #endregion

    protected BasePaginatedPage()
    {
        Items = new ObservableCollection<T>();
    }

    #region Navigation Override

    protected override async Task HandleNavigationAsync(NavigationEventArgs e)
    {
        // Determinar si necesitamos recargar
        bool shouldReload = ShouldReloadData(e);
        
        if (shouldReload || Items.Count == 0)
        {
            InvalidateCache();
            await LoadPageAsync(1, true);
        }
        else
        {
            await LoadPageAsync(CurrentPage, false);
        }
    }

    #endregion

    #region Abstract Methods

    /// <summary>
    /// Carga los datos de la página
    /// </summary>
    protected abstract Task<(System.Collections.Generic.IEnumerable<T> items, int totalCount)> LoadDataAsync(
        int page, int pageSize, string searchTerm, CancellationToken cancellationToken);

    /// <summary>
    /// Invalida el caché de datos
    /// </summary>
    protected abstract void InvalidateCache();

    /// <summary>
    /// Actualiza las estadísticas de la página
    /// </summary>
    protected abstract Task UpdateStatsAsync(CancellationToken cancellationToken);

    #endregion

    #region Virtual Methods

    /// <summary>
    /// Determina si se deben recargar los datos en la navegación
    /// </summary>
    protected virtual bool ShouldReloadData(NavigationEventArgs e)
    {
        return e.Parameter?.ToString() == "Reload";
    }

    /// <summary>
    /// Se llama cuando cambia el estado de carga
    /// </summary>
    protected virtual void OnLoadingStateChanged(bool isLoading)
    {
        // Override en clases derivadas si necesario
    }

    /// <summary>
    /// Procesa un elemento antes de agregarlo a la colección
    /// </summary>
    protected virtual T ProcessItem(T item)
    {
        return item;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Carga una página específica
    /// </summary>
    public async Task LoadPageAsync(int page, bool updateStats = true)
    {
        if (IsLoading || !IsPageActive || PageCancellationToken.IsCancellationRequested)
            return;

        try
        {
            IsLoading = true;

            var (items, totalCount) = await LoadDataAsync(page, PageSize, SearchTerm, PageCancellationToken);

            if (PageCancellationToken.IsCancellationRequested)
                return;

            await ExecuteOnUIThreadAsync(() =>
            {
                Items.Clear();
                foreach (var item in items)
                {
                    var processedItem = ProcessItem(item);
                    Items.Add(processedItem);
                }

                CurrentPage = page;
                TotalCount = totalCount;
            });

            if (updateStats && IsPageActive)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await UpdateStatsAsync(PageCancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        // Ignorar cancelaciones
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error actualizando estadísticas: {ex.Message}");
                    }
                }, PageCancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Ignorar cancelaciones
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error cargando página: {ex.Message}");
            
            if (IsPageActive)
            {
                await ShowInfoDialogAsync("Error", $"Error al cargar datos: {ex.Message}");
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Va a la primera página
    /// </summary>
    public async Task GoToFirstPageAsync()
    {
        if (HasPreviousPage && !IsLoading)
            await LoadPageAsync(1);
    }

    /// <summary>
    /// Va a la página anterior
    /// </summary>
    public async Task GoToPreviousPageAsync()
    {
        if (HasPreviousPage && !IsLoading)
            await LoadPageAsync(CurrentPage - 1);
    }

    /// <summary>
    /// Va a la página siguiente
    /// </summary>
    public async Task GoToNextPageAsync()
    {
        if (HasNextPage && !IsLoading)
            await LoadPageAsync(CurrentPage + 1);
    }

    /// <summary>
    /// Va a la última página
    /// </summary>
    public async Task GoToLastPageAsync()
    {
        if (HasNextPage && !IsLoading)
            await LoadPageAsync(TotalPages);
    }

    /// <summary>
    /// Actualiza los datos de la página actual
    /// </summary>
    public async Task RefreshAsync()
    {
        InvalidateCache();
        await LoadPageAsync(CurrentPage);
    }

    /// <summary>
    /// Realiza una búsqueda
    /// </summary>
    public async Task SearchAsync(string searchTerm)
    {
        if (SearchTerm != searchTerm)
        {
            SearchTerm = searchTerm;
            InvalidateCache();
            await LoadPageAsync(1);
        }
    }

    #endregion
}