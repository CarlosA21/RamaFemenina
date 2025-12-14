using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace RamaFemenina.Services
{
    public class PaginatedCollection<T> : ObservableCollection<T>, INotifyPropertyChanged
    {
        private readonly Func<int, int, string, CancellationToken, Task<(IEnumerable<T> items, int totalCount)>> _loadPageFunc;
        private readonly int _pageSize;
        private int _currentPage = 1;
        private int _totalCount = 0;
        private bool _isLoading = false;
        private bool _hasMore = true;
        private string _searchTerm = "";
        private CancellationTokenSource _cancellationTokenSource;

        public PaginatedCollection(
            Func<int, int, string, CancellationToken, Task<(IEnumerable<T> items, int totalCount)>> loadPageFunc,
            int pageSize = 50)
        {
            _loadPageFunc = loadPageFunc;
            _pageSize = pageSize;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public int CurrentPage
        {
            get => _currentPage;
            private set
            {
                if (_currentPage != value)
                {
                    _currentPage = value;
                    OnPropertyChanged();
                }
            }
        }

        public int TotalCount
        {
            get => _totalCount;
            private set
            {
                if (_totalCount != value)
                {
                    _totalCount = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TotalPages));
                    OnPropertyChanged(nameof(HasPreviousPage));
                    OnPropertyChanged(nameof(HasNextPage));
                }
            }
        }

        public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / _pageSize);

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasMore
        {
            get => _hasMore;
            private set
            {
                if (_hasMore != value)
                {
                    _hasMore = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;

        public string SearchTerm
        {
            get => _searchTerm;
            set
            {
                if (_searchTerm != value)
                {
                    _searchTerm = value;
                    OnPropertyChanged();
                    _ = RefreshAsync();
                }
            }
        }

        public async Task LoadInitialAsync()
        {
            CurrentPage = 1;
            await LoadPageAsync(1, replace: true);
        }

        public async Task RefreshAsync()
        {
            CurrentPage = 1;
            await LoadPageAsync(1, replace: true);
        }

        public async Task LoadNextPageAsync()
        {
            if (HasNextPage && !IsLoading)
            {
                await LoadPageAsync(CurrentPage + 1, replace: false);
            }
        }

        public async Task LoadPreviousPageAsync()
        {
            if (HasPreviousPage && !IsLoading)
            {
                await LoadPageAsync(CurrentPage - 1, replace: true);
            }
        }

        public async Task LoadSpecificPageAsync(int page)
        {
            if (page >= 1 && page <= TotalPages && !IsLoading)
            {
                await LoadPageAsync(page, replace: true);
            }
        }

        private async Task LoadPageAsync(int page, bool replace)
        {
            try
            {
                // Cancelar operación anterior si existe
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();

                IsLoading = true;

                var result = await _loadPageFunc(page, _pageSize, _searchTerm, _cancellationTokenSource.Token);

                if (_cancellationTokenSource.Token.IsCancellationRequested)
                    return;

                if (replace)
                {
                    Clear();
                }

                foreach (var item in result.items)
                {
                    Add(item);
                }

                CurrentPage = page;
                TotalCount = result.totalCount;
                HasMore = page < TotalPages;
            }
            catch (OperationCanceledException)
            {
                // Operación cancelada, ignorar
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading page {page}: {ex.Message}");
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected override event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            PropertyChanged?.Invoke(this, e);
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }
    }
}