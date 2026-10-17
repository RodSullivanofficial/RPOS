using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace RPOS
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Inicjalizacja danych z plików JSON
            DataStorage.EnsureLoginFileExists();
            DataStorage.LoadMenu();

            // Ekran logowania
            LoginForm loginForm = new LoginForm();
            if (loginForm.ShowDialog() == DialogResult.OK)
            {
                Application.Run(new MainForm(loginForm.LoggedInUser));
            }
        }
    }

    #region Models & Data Access
    public class UserAccount
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
    }

    public class MenuItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; } // Dania, Napoje, Desery, Dodatki
        public decimal Price { get; set; }

        public MenuItem()
        {
            Id = Guid.NewGuid().ToString();
        }
    }

    public class OrderItem
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Total => Price * Quantity;
    }

    public class CompletedOrder
    {
        public int TableId { get; set; }
        public DateTime Time { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Tip { get; set; }
        public decimal DiscountPercent { get; set; }
        public string PaymentMethod { get; set; } // Gotówka, Karta

        public CompletedOrder()
        {
            Time = DateTime.Now;
        }
    }

    public static class DataStorage
    {
        private static readonly string LoginFile = "login.json";
        private static readonly string MenuFile = "menu.json";

        public static List<MenuItem> MenuList { get; set; } = new List<MenuItem>();
        public static List<CompletedOrder> OrdersHistory { get; set; } = new List<CompletedOrder>();

        private static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer();

        public static void EnsureLoginFileExists()
        {
            if (!File.Exists(LoginFile))
            {
                var defaultUsers = new List<UserAccount>
                {
                    new UserAccount { Username = "root", Password = "rpos1", Role = "Manager" },
                    new UserAccount { Username = "cashier", Password = "rpos2", Role = "Kasjer" }
                };
                string json = Serializer.Serialize(defaultUsers);
                File.WriteAllText(LoginFile, json);
            }
        }

        public static List<UserAccount> LoadUsers()
        {
            EnsureLoginFileExists();
            string json = File.ReadAllText(LoginFile);
            return Serializer.Deserialize<List<UserAccount>>(json) ?? new List<UserAccount>();
        }

        public static void LoadMenu()
        {
            if (File.Exists(MenuFile))
            {
                string json = File.ReadAllText(MenuFile);
                MenuList = Serializer.Deserialize<List<MenuItem>>(json) ?? new List<MenuItem>();
            }
            else
            {
                // Domyślne menu na pierwsze uruchomienie
                MenuList = new List<MenuItem>
                {
                    new MenuItem { Name = "Burger Klasyczny", Category = "Dania", Price = 32.00m },
                    new MenuItem { Name = "Pizza Margherita", Category = "Dania", Price = 28.50m },
                    new MenuItem { Name = "Coca-Cola 0.5L", Category = "Napoje", Price = 9.00m },
                    new MenuItem { Name = "Kawa Espresso", Category = "Napoje", Price = 10.00m },
                    new MenuItem { Name = "Sernik z Malinami", Category = "Desery", Price = 18.00m },
                    new MenuItem { Name = "Frytki XL", Category = "Dodatki", Price = 12.00m }
                };
                SaveMenu();
            }
        }

        public static void SaveMenu()
        {
            string json = Serializer.Serialize(MenuList);
            File.WriteAllText(MenuFile, json);
        }
    }
    #endregion

    #region Login Form
    public class LoginForm : Form
    {
        public string LoggedInUser { get; private set; }
        private TextBox txtUser;
        private TextBox txtPass;
        private Label lblError;

        public LoginForm()
        {
            this.Text = "RPOS - Logowanie";
            this.Size = new Size(380, 280);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(40, 40, 40);

            Label lblTitle = new Label()
            {
                Text = "SYSTEM RPOS LOGOWANIE",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Left = 20,
                Top = 20,
                AutoSize = true
            };

            Label lblUser = new Label() { Text = "Użytkownik:", ForeColor = Color.LightGray, Left = 30, Top = 65, AutoSize = true };
            txtUser = new TextBox() { Left = 130, Top = 62, Width = 180, Font = new Font("Segoe UI", 10) };

            Label lblPass = new Label() { Text = "Hasło:", ForeColor = Color.LightGray, Left = 30, Top = 105, AutoSize = true };
            txtPass = new TextBox() { Left = 130, Top = 102, Width = 180, PasswordChar = '*', Font = new Font("Segoe UI", 10) };

            lblError = new Label() { ForeColor = Color.OrangeRed, Left = 30, Top = 135, AutoSize = true, Text = "" };

            Button btnLogin = new Button()
            {
                Text = "Zaloguj",
                Left = 130,
                Top = 165,
                Width = 180,
                Height = 40,
                BackColor = Color.SeaGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnLogin.Click += BtnLogin_Click;

            this.Controls.Add(lblTitle);
            this.Controls.Add(lblUser);
            this.Controls.Add(txtUser);
            this.Controls.Add(lblPass);
            this.Controls.Add(txtPass);
            this.Controls.Add(lblError);
            this.Controls.Add(btnLogin);
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            var users = DataStorage.LoadUsers();
            var match = users.FirstOrDefault(u => u.Username.Equals(txtUser.Text.Trim(), StringComparison.OrdinalIgnoreCase)
                                                && u.Password == txtPass.Text);

            if (match != null)
            {
                LoggedInUser = match.Username;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                lblError.Text = "Błędny login lub hasło!";
            }
        }
    }
    #endregion

    #region Main Form
    public partial class MainForm : Form
    {
        private Panel pnlNavigation;
        private Panel pnlContainer;
        private string _currentUser;

        public MainForm(string user)
        {
            _currentUser = user;
            SetupLayout();
        }

        private void SetupLayout()
        {
            this.Text = $"System RPOS - Zalogowano jako: {_currentUser}";
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.FromArgb(40, 40, 40);

            pnlNavigation = new Panel()
            {
                Dock = DockStyle.Left,
                Width = 200,
                BackColor = Color.FromArgb(30, 30, 30)
            };

            Button btnTables = CreateNavButton("Stoliki", 20, (s, e) => ShowTableManager());
            Button btnMenu = CreateNavButton("Katalog Menu", 80, (s, e) => ShowMenuManager());
            Button btnReports = CreateNavButton("Raporty", 140, (s, e) => ShowReports());
            Button btnLogout = CreateNavButton("Wyloguj", 220, (s, e) => Application.Restart());
            btnLogout.BackColor = Color.IndianRed;

            pnlNavigation.Controls.Add(btnTables);
            pnlNavigation.Controls.Add(btnMenu);
            pnlNavigation.Controls.Add(btnReports);
            pnlNavigation.Controls.Add(btnLogout);

            pnlContainer = new Panel()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(50, 50, 50)
            };

            this.Controls.Add(pnlContainer);
            this.Controls.Add(pnlNavigation);

            ShowTableManager();
        }

        private Button CreateNavButton(string text, int top, EventHandler onClick)
        {
            Button btn = new Button()
            {
                Text = text,
                Left = 10,
                Top = top,
                Width = 180,
                Height = 50,
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            btn.Click += onClick;
            return btn;
        }

        private void ShowTableManager()
        {
            pnlContainer.Controls.Clear();
            var tableControl = new TableViewControl(OnTableSelected) { Dock = DockStyle.Fill };
            pnlContainer.Controls.Add(tableControl);
        }

        private void OnTableSelected(int tableId)
        {
            pnlContainer.Controls.Clear();
            var orderControl = new OrderViewControl(tableId, () => ShowTableManager()) { Dock = DockStyle.Fill };
            pnlContainer.Controls.Add(orderControl);
        }

        private void ShowMenuManager()
        {
            pnlContainer.Controls.Clear();
            var menuControl = new MenuManagerControl() { Dock = DockStyle.Fill };
            pnlContainer.Controls.Add(menuControl);
        }

        private void ShowReports()
        {
            pnlContainer.Controls.Clear();
            var reportsControl = new ReportsControl() { Dock = DockStyle.Fill };
            pnlContainer.Controls.Add(reportsControl);
        }
    }
    #endregion

    #region Views

    // WIDOK STOLIKÓW
    public class TableViewControl : UserControl
    {
        private Action<int> _onTableClick;

        public TableViewControl(Action<int> onTableClick)
        {
            _onTableClick = onTableClick;
            InitializeTables();
        }

        private void InitializeTables()
        {
            this.BackColor = Color.FromArgb(45, 45, 45);
            FlowLayoutPanel flowPanel = new FlowLayoutPanel()
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(20)
            };

            for (int i = 1; i <= 12; i++)
            {
                string status = (i % 4 == 0) ? "Zajęty" : (i % 7 == 0) ? "Zarezerwowany" : "Wolny";
                Color btnColor = status == "Wolny" ? Color.SeaGreen : status == "Zajęty" ? Color.IndianRed : Color.DarkOrange;

                Button btnTable = new Button()
                {
                    Text = $"Stolik #{i}\n[{status}]",
                    Width = 150,
                    Height = 120,
                    BackColor = btnColor,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 11, FontStyle.Bold),
                    Tag = i,
                    Cursor = Cursors.Hand
                };

                btnTable.Click += (s, e) =>
                {
                    int tableId = (int)((Button)s).Tag;
                    _onTableClick?.Invoke(tableId);
                };

                flowPanel.Controls.Add(btnTable);
            }

            this.Controls.Add(flowPanel);
        }
    }

    // WIDOK ZAMÓWIENIA & KOSZYKA
    public class OrderViewControl : UserControl
    {
        private int _tableId;
        private Action _backCallback;
        private List<OrderItem> _cart = new List<OrderItem>();
        private DataGridView dgvCart;
        private FlowLayoutPanel flowProducts;
        private Label lblTotal;
        private NumericUpDown numDiscount;
        private NumericUpDown numTip;

        public OrderViewControl(int tableId, Action backCallback)
        {
            _tableId = tableId;
            _backCallback = backCallback;
            InitializeOrderScreen();
        }

        private void InitializeOrderScreen()
        {
            this.BackColor = Color.FromArgb(50, 50, 50);

            Button btnBack = new Button()
            {
                Text = "← Powrót",
                Left = 20,
                Top = 15,
                Width = 120,
                Height = 35,
                BackColor = Color.Gray,
                ForeColor = Color.White
            };
            btnBack.Click += (s, e) => _backCallback?.Invoke();

            Label lblTitle = new Label()
            {
                Text = $"Zamówienie - Stolik #{_tableId}",
                Left = 160,
                Top = 18,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true
            };

            // Grid Koszyka
            dgvCart = new DataGridView()
            {
                Left = 20,
                Top = 60,
                Width = 450,
                Height = 380,
                BackgroundColor = Color.White,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AllowUserToAddRows = false
            };
            dgvCart.Columns.Add("Name", "Nazwa");
            dgvCart.Columns.Add("Qty", "Ilość");
            dgvCart.Columns.Add("Price", "Cena");
            dgvCart.Columns.Add("Total", "Suma");

            Button btnRemove = new Button()
            {
                Text = "Usuń zaznaczoną pozycję",
                Left = 20,
                Top = 450,
                Width = 200,
                Height = 35,
                BackColor = Color.IndianRed,
                ForeColor = Color.White
            };
            btnRemove.Click += (s, e) => RemoveSelectedItem();

            Label lblDisc = new Label() { Text = "Rabat (%):", Left = 240, Top = 455, ForeColor = Color.White, AutoSize = true };
            numDiscount = new NumericUpDown() { Left = 310, Top = 453, Width = 50, Minimum = 0, Maximum = 100 };
            numDiscount.ValueChanged += (s, e) => UpdateCartGrid();

            Label lblTip = new Label() { Text = "Napiwek (zł):", Left = 370, Top = 455, ForeColor = Color.White, AutoSize = true };
            numTip = new NumericUpDown() { Left = 450, Top = 453, Width = 60, Minimum = 0, Maximum = 500, DecimalPlaces = 2 };
            numTip.ValueChanged += (s, e) => UpdateCartGrid();

            lblTotal = new Label()
            {
                Text = "RAZEM: 0,00 zł",
                Left = 20,
                Top = 500,
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.Gold,
                AutoSize = true
            };

            Button btnPayCash = new Button()
            {
                Text = "Płatność GOTÓWKA",
                Left = 20,
                Top = 540,
                Width = 210,
                Height = 50,
                BackColor = Color.SeaGreen,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            btnPayCash.Click += (s, e) => CompleteOrder("Gotówka");

            Button btnPayCard = new Button()
            {
                Text = "Płatność KARTA",
                Left = 240,
                Top = 540,
                Width = 210,
                Height = 50,
                BackColor = Color.SteelBlue,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            btnPayCard.Click += (s, e) => CompleteOrder("Karta");

            // Panel Produktów z Menu
            flowProducts = new FlowLayoutPanel()
            {
                Left = 520,
                Top = 60,
                Width = 550,
                Height = 530,
                AutoScroll = true,
                BackColor = Color.FromArgb(40, 40, 40),
                Padding = new Padding(10)
            };

            LoadMenuButtons();

            this.Controls.Add(btnBack);
            this.Controls.Add(lblTitle);
            this.Controls.Add(dgvCart);
            this.Controls.Add(btnRemove);
            this.Controls.Add(lblDisc);
            this.Controls.Add(numDiscount);
            this.Controls.Add(lblTip);
            this.Controls.Add(numTip);
            this.Controls.Add(lblTotal);
            this.Controls.Add(btnPayCash);
            this.Controls.Add(btnPayCard);
            this.Controls.Add(flowProducts);
        }

        private void LoadMenuButtons()
        {
            flowProducts.Controls.Clear();
            foreach (var item in DataStorage.MenuList)
            {
                Button btn = new Button()
                {
                    Text = $"{item.Name}\n{item.Price:F2} zł",
                    Width = 150,
                    Height = 90,
                    BackColor = Color.FromArgb(70, 70, 90),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    Tag = item
                };
                btn.Click += (s, e) => AddToCart((MenuItem)((Button)s).Tag);
                flowProducts.Controls.Add(btn);
            }
        }

        private void AddToCart(MenuItem item)
        {
            var existing = _cart.FirstOrDefault(c => c.Name == item.Name);
            if (existing != null)
            {
                existing.Quantity++;
            }
            else
            {
                _cart.Add(new OrderItem { Name = item.Name, Price = item.Price, Quantity = 1 });
            }
            UpdateCartGrid();
        }

        private void RemoveSelectedItem()
        {
            if (dgvCart.CurrentRow != null)
            {
                int index = dgvCart.CurrentRow.Index;
                if (index >= 0 && index < _cart.Count)
                {
                    _cart.RemoveAt(index);
                    UpdateCartGrid();
                }
            }
        }

        private void UpdateCartGrid()
        {
            dgvCart.Rows.Clear();
            decimal subtotal = 0;
            foreach (var item in _cart)
            {
                dgvCart.Rows.Add(item.Name, item.Quantity, $"{item.Price:F2} zł", $"{item.Total:F2} zł");
                subtotal += item.Total;
            }

            decimal discountPercent = numDiscount.Value;
            decimal discountAmount = subtotal * (discountPercent / 100m);
            decimal finalTotal = (subtotal - discountAmount) + numTip.Value;

            lblTotal.Text = $"RAZEM: {finalTotal:F2} zł";
        }

        private void CompleteOrder(string method)
        {
            if (_cart.Count == 0)
            {
                MessageBox.Show("Koszyk jest pusty!", "Informacja", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal subtotal = _cart.Sum(x => x.Total);
            decimal finalTotal = (subtotal * (1m - (numDiscount.Value / 100m))) + numTip.Value;

            var order = new CompletedOrder()
            {
                TableId = _tableId,
                TotalAmount = finalTotal,
                DiscountPercent = numDiscount.Value,
                Tip = numTip.Value,
                PaymentMethod = method
            };

            DataStorage.OrdersHistory.Add(order);

            MessageBox.Show($"Zamówienie opłacone ({method})!\nSuma: {finalTotal:F2} zł\n\nWydrukowano paragon.", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _backCallback?.Invoke();
        }
    }

    // MODUŁ ZARZĄDZANIA MENU
    public class MenuManagerControl : UserControl
    {
        private DataGridView dgvMenu;
        private TextBox txtName;
        private TextBox txtPrice;
        private ComboBox cbCategory;

        public MenuManagerControl()
        {
            InitializeMenuManager();
        }

        private void InitializeMenuManager()
        {
            this.BackColor = Color.FromArgb(50, 50, 50);

            Label lblTitle = new Label()
            {
                Text = "Zarządzanie Katalogiem Menu",
                Left = 20,
                Top = 20,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true
            };

            dgvMenu = new DataGridView()
            {
                Left = 20,
                Top = 60,
                Width = 500,
                Height = 450,
                BackgroundColor = Color.White,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            Label lblAdd = new Label() { Text = "Dodaj Nową Pozycję:", Left = 550, Top = 60, Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.White, AutoSize = true };

            Label lblName = new Label() { Text = "Nazwa dania/napoju:", Left = 550, Top = 100, ForeColor = Color.White, AutoSize = true };
            txtName = new TextBox() { Left = 550, Top = 125, Width = 250, Font = new Font("Segoe UI", 10) };

            Label lblCategory = new Label() { Text = "Kategoria:", Left = 550, Top = 165, ForeColor = Color.White, AutoSize = true };
            cbCategory = new ComboBox() { Left = 550, Top = 190, Width = 250, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10) };
            cbCategory.Items.AddRange(new object[] { "Dania", "Napoje", "Desery", "Dodatki" });
            cbCategory.SelectedIndex = 0;

            Label lblPrice = new Label() { Text = "Cena (zł):", Left = 550, Top = 230, ForeColor = Color.White, AutoSize = true };
            txtPrice = new TextBox() { Left = 550, Top = 255, Width = 250, Font = new Font("Segoe UI", 10) };

            Button btnAdd = new Button()
            {
                Text = "+ Dodaj do Menu",
                Left = 550,
                Top = 300,
                Width = 250,
                Height = 45,
                BackColor = Color.SeaGreen,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnAdd.Click += BtnAdd_Click;

            Button btnDelete = new Button()
            {
                Text = "Usuń zaznaczoną pozycję",
                Left = 20,
                Top = 520,
                Width = 220,
                Height = 40,
                BackColor = Color.IndianRed,
                ForeColor = Color.White
            };
            btnDelete.Click += BtnDelete_Click;

            this.Controls.Add(lblTitle);
            this.Controls.Add(dgvMenu);
            this.Controls.Add(lblAdd);
            this.Controls.Add(lblName);
            this.Controls.Add(txtName);
            this.Controls.Add(lblCategory);
            this.Controls.Add(cbCategory);
            this.Controls.Add(lblPrice);
            this.Controls.Add(txtPrice);
            this.Controls.Add(btnAdd);
            this.Controls.Add(btnDelete);

            RefreshGrid();
        }

        private void RefreshGrid()
        {
            dgvMenu.DataSource = null;
            dgvMenu.DataSource = DataStorage.MenuList.Select(m => new { Nazwa = m.Name, Kategoria = m.Category, Cena = $"{m.Price:F2} zł" }).ToList();
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text) || !decimal.TryParse(txtPrice.Text, out decimal price))
            {
                MessageBox.Show("Wprowadź poprawną nazwę i cenę!", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DataStorage.MenuList.Add(new MenuItem()
            {
                Name = txtName.Text.Trim(),
                Category = cbCategory.SelectedItem.ToString(),
                Price = price
            });

            DataStorage.SaveMenu();
            txtName.Clear();
            txtPrice.Clear();
            RefreshGrid();
            MessageBox.Show("Dodano nową pozycję do menu!", "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dgvMenu.CurrentRow != null)
            {
                int index = dgvMenu.CurrentRow.Index;
                if (index >= 0 && index < DataStorage.MenuList.Count)
                {
                    DataStorage.MenuList.RemoveAt(index);
                    DataStorage.SaveMenu();
                    RefreshGrid();
                }
            }
        }
    }

    // MODUŁ RAPORTÓW
    public class ReportsControl : UserControl
    {
        private Label lblTotalSales;
        private Label lblOrderCount;
        private Label lblCashTotal;
        private Label lblCardTotal;
        private Label lblTipTotal;
        private DataGridView dgvHistory;

        public ReportsControl()
        {
            InitializeReports();
        }

        private void InitializeReports()
        {
            this.BackColor = Color.FromArgb(50, 50, 50);

            Label lblTitle = new Label()
            {
                Text = "Raporty Sprzedażowe / Dobowe",
                Left = 20,
                Top = 20,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true
            };

            Panel pnlStats = new Panel()
            {
                Left = 20,
                Top = 60,
                Width = 300,
                Height = 480,
                BackColor = Color.FromArgb(40, 40, 40),
                Padding = new Padding(15)
            };

            lblOrderCount = CreateStatLabel("Obsłużone zamówienia: 0", 20, pnlStats);
            lblTotalSales = CreateStatLabel("Łączny obrót: 0,00 zł", 60, pnlStats);
            lblCashTotal = CreateStatLabel("Płatność Gotówką: 0,00 zł", 110, pnlStats);
            lblCardTotal = CreateStatLabel("Płatność Kartą: 0,00 zł", 150, pnlStats);
            lblTipTotal = CreateStatLabel("Suma napiwków: 0,00 zł", 200, pnlStats);

            dgvHistory = new DataGridView()
            {
                Left = 340,
                Top = 60,
                Width = 650,
                Height = 480,
                BackgroundColor = Color.White,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true
            };

            this.Controls.Add(lblTitle);
            this.Controls.Add(pnlStats);
            this.Controls.Add(dgvHistory);

            CalculateReports();
        }

        private Label CreateStatLabel(string text, int top, Panel parent)
        {
            Label lbl = new Label()
            {
                Text = text,
                Left = 15,
                Top = top,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.Gold,
                AutoSize = true
            };
            parent.Controls.Add(lbl);
            return lbl;
        }

        private void CalculateReports()
        {
            var history = DataStorage.OrdersHistory;

            int count = history.Count;
            decimal total = history.Sum(h => h.TotalAmount);
            decimal cash = history.Where(h => h.PaymentMethod == "Gotówka").Sum(h => h.TotalAmount);
            decimal card = history.Where(h => h.PaymentMethod == "Karta").Sum(h => h.TotalAmount);
            decimal tips = history.Sum(h => h.Tip);

            lblOrderCount.Text = $"Obsłużone zamówienia: {count}";
            lblTotalSales.Text = $"Łączny obrót: {total:F2} zł";
            lblCashTotal.Text = $"Płatność Gotówką: {cash:F2} zł";
            lblCardTotal.Text = $"Płatność Kartą: {card:F2} zł";
            lblTipTotal.Text = $"Suma napiwków: {tips:F2} zł";

            dgvHistory.DataSource = null;
            dgvHistory.DataSource = history.Select(h => new
            {
                Czas = h.Time.ToString("HH:mm:ss"),
                Stolik = $"Stolik #{h.TableId}",
                Metoda = h.PaymentMethod,
                Rabat = $"{h.DiscountPercent}%",
                Napiwek = $"{h.Tip:F2} zł",
                Kwota = $"{h.TotalAmount:F2} zł"
            }).ToList();
        }
    }
    #endregion
}