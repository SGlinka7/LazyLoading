using System.ComponentModel;
using LazyLoading.Core;

namespace LazyLoading.Controls
{
    /// <summary>
    /// DataGridView z wbudowanym mechanizmem lazy loading i paginacji SQL.
    /// Automatycznie ładuje dane przy scrollowaniu.
    /// </summary>
    public class PagedDataGridView : DataGridView
    {
        private VirtualScrollHandler? _scrollHandler;
        private PagedDataSource? _pagedDataSource;
        private System.Windows.Forms.Timer? _scrollTimer;
        private Label? _statusLabel;
        private ProgressBar? _progressBar;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public PagedDataSource? PagedDataSource
        {
            get => _pagedDataSource;
            set
            {
                if (_pagedDataSource != value)
                {
                    _pagedDataSource = value;
                    OnDataSourceChanged();
                }
            }
        }

        [Category("Paging")]
        [Description("Pokazuje pasek postępu podczas ładowania danych")]
        [DefaultValue(true)]
        public bool ShowLoadingIndicator { get; set; } = true;

        [Category("Paging")]
        [Description("Pokazuje status paginacji (liczba załadowanych rekordów)")]
        [DefaultValue(true)]
        public bool ShowStatusLabel { get; set; } = true;

        [Category("Paging")]
        [Description("Opóźnienie przed załadowaniem danych przy scrollowaniu (ms)")]
        [DefaultValue(150)]
        public int ScrollDebounceDelay { get; set; } = 150;

        public PagedDataGridView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // Optymalizacje dla wydajności
            this.DoubleBuffered = true;
            this.AllowUserToAddRows = false;
            this.AllowUserToDeleteRows = false;
            this.ReadOnly = true;
            this.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            // Timer do debounce scrollowania
            _scrollTimer = new System.Windows.Forms.Timer();
            _scrollTimer.Interval = ScrollDebounceDelay;
            _scrollTimer.Tick += ScrollTimer_Tick;

            // Event handlery
            this.Scroll += PagedDataGridView_Scroll;
        }

        private async void OnDataSourceChanged()
        {
            if (_pagedDataSource == null)
            {
                this.DataSource = null;
                _scrollHandler = null;
                return;
            }

            _scrollHandler = new VirtualScrollHandler(_pagedDataSource);
            _scrollHandler.LoadingStateChanged += ScrollHandler_LoadingStateChanged;

            _pagedDataSource.PageLoaded += PagedDataSource_PageLoaded;
            _pagedDataSource.ErrorOccurred += PagedDataSource_ErrorOccurred;

            await InitializeDataAsync();
        }

        private async Task InitializeDataAsync()
        {
            if (_scrollHandler == null)
                return;

            try
            {
                ShowLoading(true);

                await _scrollHandler.InitializeAsync();

                this.DataSource = _scrollHandler.GetDataTable();

                UpdateStatusLabel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas ładowania danych: {ex.Message}",
                    "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private void PagedDataGridView_Scroll(object? sender, ScrollEventArgs e)
        {
            if (e.ScrollOrientation == ScrollOrientation.VerticalScroll)
            {
                // Restart timer dla debounce
                _scrollTimer?.Stop();
                _scrollTimer?.Start();
            }
        }

        private async void ScrollTimer_Tick(object? sender, EventArgs e)
        {
            _scrollTimer?.Stop();

            if (_scrollHandler != null && this.FirstDisplayedScrollingRowIndex >= 0)
            {
                await _scrollHandler.HandleScrollAsync(
                    this.FirstDisplayedScrollingRowIndex,
                    this.DisplayedRowCount(false));

                UpdateStatusLabel();
            }
        }

        private void ScrollHandler_LoadingStateChanged(object? sender, LoadingStateChangedEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(() => ShowLoading(e.IsLoading));
            }
            else
            {
                ShowLoading(e.IsLoading);
            }
        }

        private void PagedDataSource_PageLoaded(object? sender, Core.PageLoadedEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(() => UpdateStatusLabel());
            }
            else
            {
                UpdateStatusLabel();
            }
        }

        private void PagedDataSource_ErrorOccurred(object? sender, Core.ErrorEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(() =>
                    MessageBox.Show(e.ErrorMessage, "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error));
            }
            else
            {
                MessageBox.Show(e.ErrorMessage, "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowLoading(bool isLoading)
        {
            if (!ShowLoadingIndicator)
                return;

            if (_progressBar == null && isLoading)
            {
                CreateProgressBar();
            }

            if (_progressBar != null)
            {
                _progressBar.Visible = isLoading;
            }

            this.Cursor = isLoading ? Cursors.WaitCursor : Cursors.Default;
        }

        private void CreateProgressBar()
        {
            if (this.Parent == null)
                return;

            _progressBar = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Height = 20,
                Visible = false
            };

            this.Parent.Controls.Add(_progressBar);
            _progressBar.BringToFront();

            // Pozycjonuj na dole grida
            PositionProgressBar();
            this.Resize += (s, e) => PositionProgressBar();
        }

        private void PositionProgressBar()
        {
            if (_progressBar != null)
            {
                _progressBar.Location = new Point(this.Left, this.Bottom - _progressBar.Height - 5);
                _progressBar.Width = this.Width;
            }
        }

        private void UpdateStatusLabel()
        {
            if (!ShowStatusLabel || _scrollHandler == null || _pagedDataSource == null)
                return;

            if (_statusLabel == null)
            {
                CreateStatusLabel();
            }

            if (_statusLabel != null)
            {
                int loadedRows = _scrollHandler.GetDataTable().Rows.Count;
                int totalRows = _pagedDataSource.TotalRecords;

                _statusLabel.Text = $"Załadowano: {loadedRows:N0} / {totalRows:N0} rekordów";
            }
        }

        private void CreateStatusLabel()
        {
            if (this.Parent == null)
                return;

            _statusLabel = new Label
            {
                AutoSize = true,
                BackColor = SystemColors.Info,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(5),
                Visible = true
            };

            this.Parent.Controls.Add(_statusLabel);
            _statusLabel.BringToFront();

            // Pozycjonuj w prawym dolnym rogu
            PositionStatusLabel();
            this.Resize += (s, e) => PositionStatusLabel();
        }

        private void PositionStatusLabel()
        {
            if (_statusLabel != null)
            {
                _statusLabel.Location = new Point(
                    this.Right - _statusLabel.Width - 10,
                    this.Bottom - _statusLabel.Height - 10);
            }
        }

        /// <summary>
        /// Odświeża dane - ładuje od początku.
        /// </summary>
        public async Task RefreshDataAsync()
        {
            if (_scrollHandler != null)
            {
                ShowLoading(true);
                try
                {
                    await _scrollHandler.RefreshAsync();
                    UpdateStatusLabel();
                }
                finally
                {
                    ShowLoading(false);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _scrollTimer?.Dispose();
                _progressBar?.Dispose();
                _statusLabel?.Dispose();

                if (_pagedDataSource != null)
                {
                    _pagedDataSource.PageLoaded -= PagedDataSource_PageLoaded;
                    _pagedDataSource.ErrorOccurred -= PagedDataSource_ErrorOccurred;
                }

                if (_scrollHandler != null)
                {
                    _scrollHandler.LoadingStateChanged -= ScrollHandler_LoadingStateChanged;
                }
            }

            base.Dispose(disposing);
        }
    }
}
