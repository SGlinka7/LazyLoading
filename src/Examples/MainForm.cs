using LazyLoading.Controls;
using LazyLoading.Core;

namespace LazyLoading.Examples
{
    /// <summary>
    /// Przykładowy formularz demonstrujący użycie PagedDataGridView.
    /// </summary>
    public partial class MainForm : Form
    {
        private PagedDataGridView pagedGridView;
        private Button btnRefresh;
        private Button btnFilter;
        private TextBox txtSearch;
        private ComboBox cmbCategory;
        private Label lblInfo;

        // Connection string - ZMIEŃ NA SWÓJ!
        private const string CONNECTION_STRING = "Server=localhost;Database=YourDatabaseName;Integrated Security=true;";

        public MainForm()
        {
            InitializeComponent();
            InitializePagedGrid();
        }

        private void InitializeComponent()
        {
            this.Text = "Lazy Loading Demo - Paginacja SQL";
            this.Size = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Panel górny z kontrolkami
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                Padding = new Padding(10)
            };

            lblInfo = new Label
            {
                Text = "Wpisz tekst lub wybierz kategorię i kliknij 'Filtruj'",
                Location = new Point(10, 10),
                AutoSize = true
            };

            txtSearch = new TextBox
            {
                Location = new Point(10, 35),
                Width = 200,
                PlaceholderText = "Szukaj produktu..."
            };

            cmbCategory = new ComboBox
            {
                Location = new Point(220, 35),
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbCategory.Items.AddRange(new object[]
            {
                "-- Wszystkie --",
                "Electronics",
                "Books",
                "Clothing",
                "Food",
                "Toys",
                "Sports",
                "Home & Garden",
                "Automotive",
                "Health",
                "Beauty"
            });
            cmbCategory.SelectedIndex = 0;

            btnFilter = new Button
            {
                Text = "Filtruj",
                Location = new Point(380, 35),
                Width = 100
            };
            btnFilter.Click += BtnFilter_Click;

            btnRefresh = new Button
            {
                Text = "Odśwież",
                Location = new Point(490, 35),
                Width = 100
            };
            btnRefresh.Click += BtnRefresh_Click;

            topPanel.Controls.AddRange(new Control[]
            {
                lblInfo, txtSearch, cmbCategory, btnFilter, btnRefresh
            });

            this.Controls.Add(topPanel);

            // Info panel na dole
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 100,
                Padding = new Padding(10)
            };

            var lblInstructions = new Label
            {
                Text = "💡 INSTRUKCJA:\n" +
                       "• Scroll w dół aby automatycznie załadować więcej danych\n" +
                       "• Dane ładują się po 50 rekordów (zmień pageSize w kodzie)\n" +
                       "• Status załadowanych rekordów widoczny w prawym dolnym rogu",
                Dock = DockStyle.Fill,
                Font = new Font(this.Font.FontFamily, 9),
                ForeColor = Color.DarkBlue
            };

            bottomPanel.Controls.Add(lblInstructions);
            this.Controls.Add(bottomPanel);
        }

        private void InitializePagedGrid()
        {
            // Utwórz PagedDataGridView
            pagedGridView = new PagedDataGridView
            {
                Dock = DockStyle.Fill,
                ShowLoadingIndicator = true,
                ShowStatusLabel = true,
                ScrollDebounceDelay = 150,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            this.Controls.Add(pagedGridView);

            // Załaduj dane początkowe
            LoadData();
        }

        private void LoadData(string? searchText = null, string? category = null)
        {
            try
            {
                PagedDataSource dataSource;

                // Jeśli są filtry, użyj filtrowanego data source
                if (!string.IsNullOrWhiteSpace(searchText) ||
                    (!string.IsNullOrWhiteSpace(category) && category != "-- Wszystkie --"))
                {
                    var filteredDataSource = new ProductsByCategoryDataSource(CONNECTION_STRING, pageSize: 50)
                    {
                        SearchText = string.IsNullOrWhiteSpace(searchText) ? null : searchText,
                        Category = (category == "-- Wszystkie --") ? null : category
                    };
                    dataSource = filteredDataSource;
                }
                else
                {
                    // Podstawowy data source bez filtrów
                    dataSource = new ProductsDataSource(CONNECTION_STRING, pageSize: 50);
                }

                // Ustaw data source - automatycznie załaduje pierwszą stronę
                pagedGridView.PagedDataSource = dataSource;

                lblInfo.Text = "Dane załadowane. Scroll w dół aby załadować więcej...";
                lblInfo.ForeColor = Color.Green;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Błąd podczas ładowania danych:\n{ex.Message}\n\n" +
                    $"Upewnij się że:\n" +
                    $"1. Connection string jest poprawny\n" +
                    $"2. Baza danych istnieje\n" +
                    $"3. Wykonano skrypty SQL z folderu Database",
                    "Błąd",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                lblInfo.Text = "Błąd podczas ładowania danych!";
                lblInfo.ForeColor = Color.Red;
            }
        }

        private void BtnFilter_Click(object? sender, EventArgs e)
        {
            string? searchText = string.IsNullOrWhiteSpace(txtSearch.Text) ? null : txtSearch.Text.Trim();
            string? category = cmbCategory.SelectedItem?.ToString();

            LoadData(searchText, category);
        }

        private async void BtnRefresh_Click(object? sender, EventArgs e)
        {
            try
            {
                btnRefresh.Enabled = false;
                await pagedGridView.RefreshDataAsync();
                lblInfo.Text = "Dane odświeżone!";
                lblInfo.ForeColor = Color.Green;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas odświeżania: {ex.Message}",
                    "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnRefresh.Enabled = true;
            }
        }
    }
}
