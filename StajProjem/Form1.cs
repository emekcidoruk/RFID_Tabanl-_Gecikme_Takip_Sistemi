using Impinj.OctaneSdk;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace StajProjem
{
    public class ComboBoxItem
    {
        public string Text { get; set; }
        public int Value { get; set; }
        public override string ToString()
        {
            return Text;
        }
    }

    public partial class Form1 : Form
    {
        // RFID Okuyucu ve Durum Değişkenleri
        private ImpinjReader reader = new ImpinjReader();
        private bool isReading = false;
        private int validatedKullaniciId = -1;

        // Admin değişkenleri
        private bool isAdminUser = false;
        private const string ADMIN_RFID_TAG = "1000 4084 0000 0000 0000 0122 142F C047";
        
        // Uygulama durumları
        private enum AppScreen
        {
            Login,         // Giriş ekranı
            MainMenu,      // Ana menü
            DualPanel,     // Çift panel ekranı (ürünler ve tamamlanmamış işlemler)
            ProductSelection, // Ürün seçimi
            DelayReason,   // Gecikme nedeni seçimi
            IncompleteTransactions, // Tamamlanmamış işlemler
            Confirmation,  // Onay ekranı
            AdminPanel     // Admin panel ekranı
        }
        private AppScreen currentScreen = AppScreen.Login;

        // RFID okuyucu ayarları
        private const string READER_IP_ADDRESS = "192.168.0.20";

        // Sayaç Değişkenleri
        private int saniye = 0;
        private int dakika = 0;
        private int saat = 0;
        private DateTime baslangicZamani;

        // Veritabanı Değişkenleri
        private const string CONNECTION_STRING = "Data Source=DORUKEMEKCI;Initial Catalog=StajProjem;Integrated Security=True";
        private int selectedUrunId = -1;
        private int selectedGecikmeId = -1;
        private int yeniKayitID = -1;
        private bool isIncompleteTransactionSelected = false;
        private int selectedAdminRecordId = -1;

        // UI Kontrolleri
        private Label lblTitle;
        private Label lblSubtitle;
        private Label lblTimer;
        private Label lblStatus;
        private DataGridView dgvData;
        private DataGridView dgvIncomplete;
        private DataGridView dgvReasons;
        private Button btnBack;
        private Button btnConfirm;
        private Button btnCancel;
        private Button btnIncomplete;
        private System.Windows.Forms.Timer timer;
        private System.Windows.Forms.Timer statusTimer;

        // Admin Filtre Kontrolleri
        private ComboBox cmbUrunFilter;
        private ComboBox cmbSebepFilter;
        private DateTimePicker dtpBaslangicFilter;
        private DateTimePicker dtpBitisFilter;
        private Button btnFilter;
        private Button btnClearFilter;

        // Dashboard Kontrolleri
        private Label lblDashboardTitle;
        private Label lblTotalTime;
        private Label lblTotalTimeValue;

        public Form1()
        {
            InitializeComponent();
            InitializeUI();
            ImpinjOkuyucuBaslat();
            VeritabaniBaglantiTestEt();
            
            // Form boyutu değiştiğinde yazıları yeniden konumlandır
            this.Resize += Form1_Resize;
            
            ShowScreen(AppScreen.Login);
        }

        private void InitializeUI()
        {
            // Form ayarları - Tam ekran
            this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Text = "Gecikme Takip Sistemi";
            this.BackColor = Color.LightGray;

            // Başlık
            lblTitle = new Label
            {
                Font = new Font("Arial", 36, FontStyle.Bold),
                ForeColor = Color.Black,
                Text = "GECİKME TAKİP SİSTEMİ",
                AutoSize = true,
                Location = new Point(0, 0)
            };

            // Alt başlık
            lblSubtitle = new Label
            {
                Font = new Font("Arial", 24, FontStyle.Regular),
                ForeColor = Color.Black,
                Text = "RFID Kartınızı Tarayınız",
                AutoSize = true,
                Location = new Point(0, 0)
            };

            // Timer label kaldırıldı

            // Status label
            lblStatus = new Label
            {
                Font = new Font("Arial", 14, FontStyle.Regular),
                ForeColor = Color.Red,
                Text = "",
                AutoSize = true,
                Location = new Point(50, 120),
                Visible = false
            };

            // DataGridView (Sol taraf - Ürün ve Sebep seçimi)
            dgvData = new DataGridView
            {
                Location = new Point(50, 180),
                Size = new Size(600, 500),
                BackgroundColor = Color.White,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                Font = new Font("Arial", 12, FontStyle.Regular),
                RowTemplate = { Height = 50 },
                ColumnHeadersHeight = 60
            };
            
            // DataGridView (Sağ taraf - Tamamlanmamış işlemler)
            dgvIncomplete = new DataGridView
            {
                Location = new Point(700, 180),
                Size = new Size(600, 500),
                BackgroundColor = Color.White,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                Font = new Font("Arial", 12, FontStyle.Regular),
                RowTemplate = { Height = 50 },
                ColumnHeadersHeight = 60
            };
            
            // DataGridView (Sebep seçimi için)
            dgvReasons = new DataGridView
            {
                Location = new Point(50, 400),
                Size = new Size(600, 200),
                BackgroundColor = Color.White,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                Font = new Font("Arial", 12, FontStyle.Regular),
                RowTemplate = { Height = 40 },
                ColumnHeadersHeight = 50,
                Visible = false
            };
            
            // Butonlar - Alt kısımda
            btnBack = new Button
            {
                Text = "GERİ",
                Size = new Size(120, 50),
                Location = new Point(50, 720),
                Font = new Font("Arial", 12, FontStyle.Bold),
                Visible = false
            };

            btnConfirm = new Button
            {
                Text = "ONAYLA",
                Size = new Size(120, 50),
                Location = new Point(190, 720),
                Font = new Font("Arial", 12, FontStyle.Bold),
                Visible = false
            };

            btnCancel = new Button
            {
                Text = "İPTAL",
                Size = new Size(120, 50),
                Location = new Point(330, 720),
                Font = new Font("Arial", 12, FontStyle.Bold),
                Visible = false
            };

            btnIncomplete = new Button
            {
                Text = "TAMAMLANMAMIŞ",
                Size = new Size(180, 50),
                Location = new Point(470, 720),
                Font = new Font("Arial", 12, FontStyle.Bold),
                Visible = false
            };

            // Event handlers
            dgvData.CellDoubleClick += DgvData_CellDoubleClick;
            dgvIncomplete.CellDoubleClick += DgvIncomplete_CellDoubleClick;
            dgvReasons.CellDoubleClick += DgvReasons_CellDoubleClick;
            btnBack.Click += BtnBack_Click;
            btnConfirm.Click += BtnConfirm_Click;
            btnCancel.Click += BtnCancel_Click;
            btnIncomplete.Click += BtnIncomplete_Click;

                    // Status timer
        statusTimer = new System.Windows.Forms.Timer();
        statusTimer.Interval = 3000;
        statusTimer.Tick += (s, args) =>
        {
            lblStatus.Visible = false;
            statusTimer.Stop();
        };

        // Incomplete transactions update timer
        timer = new System.Windows.Forms.Timer();
        timer.Interval = 1000; // 1 saniye
        timer.Tick += (s, args) =>
        {
            if (currentScreen == AppScreen.DualPanel && dgvIncomplete.Visible)
            {
                UpdateIncompleteTransactionsTimer();
            }
            else if (currentScreen == AppScreen.AdminPanel && dgvData.Visible)
            {
                UpdateAdminRecordsTimer();
            }
        };

            // Controls ekle
            this.Controls.Add(lblTitle);
            this.Controls.Add(lblSubtitle);
            this.Controls.Add(lblStatus);
            this.Controls.Add(dgvData);
            this.Controls.Add(dgvIncomplete);
            this.Controls.Add(dgvReasons);
            this.Controls.Add(btnBack);
            this.Controls.Add(btnConfirm);
            this.Controls.Add(btnCancel);
            this.Controls.Add(btnIncomplete);
        }

        private void ShowStatusMessage(string message, Color color)
        {
            lblStatus.Text = message;
            lblStatus.ForeColor = color;
            lblStatus.Visible = true;
            statusTimer.Stop();
            statusTimer.Start();
        }

        private void ShowScreen(AppScreen screen)
        {
            currentScreen = screen;
            ResetUI();
            
            switch (screen)
            {
                case AppScreen.Login:
                    ShowLoginScreen();
                    break;
                case AppScreen.MainMenu:
                    ShowMainMenuScreen();
                    break;
                case AppScreen.DualPanel:
                    ShowDualPanelScreen();
                    break;
                case AppScreen.ProductSelection:
                    ShowProductSelectionScreen();
                    break;
                case AppScreen.DelayReason:
                    ShowDelayReasonScreen();
                    break;
                case AppScreen.IncompleteTransactions:
                    ShowIncompleteTransactionsScreen();
                    break;
                case AppScreen.Confirmation:
                    ShowConfirmationScreen();
                    break;
                case AppScreen.AdminPanel:
                    ShowAdminPanelScreen();
                    break;
            }
        }

        private void ResetUI()
        {
            // Reset all buttons
            btnBack.Visible = false;
            btnConfirm.Visible = false;
            btnCancel.Visible = false;
            btnIncomplete.Visible = false;

            // Reset labels - Ana başlık ve alt başlık her zaman görünür olmalı
            lblTitle.Visible = true;
            lblSubtitle.Visible = true;
            lblStatus.Visible = false;

            // Reset data
            dgvData.Visible = false;
            dgvData.DataSource = null;
            dgvData.Columns.Clear();
            dgvIncomplete.Visible = false;
            dgvIncomplete.DataSource = null;
            dgvIncomplete.Columns.Clear();
            dgvReasons.Visible = false;
            dgvReasons.DataSource = null;
            dgvReasons.Columns.Clear();

            // DataGridView konumlarını sıfırla
            dgvData.Location = new Point(50, 180);
            dgvData.Size = new Size(600, 500);
            dgvIncomplete.Location = new Point(700, 180);
            dgvIncomplete.Size = new Size(600, 500);
            dgvReasons.Location = new Point(50, 400);
            dgvReasons.Size = new Size(600, 200);

                         // Temizlik: Dinamik olarak eklenen başlık etiketlerini kaldır
             var controlsToRemove = this.Controls.OfType<Label>().Where(l => 
                 l.Text == "ÜRÜN VE SEBEP SEÇİMİ" || 
                 l.Text == "ÜRÜNLER" ||
                 l.Text == "SEBEPLER" ||
                 l.Text == "TAMAMLANMAMIŞ İŞLEMLER" ||
                 l.Text == "FİLTRE PANELİ" ||
                 l.Text == "DASHBOARD" ||
                 l.Text == "Toplam Geçen Süre:" ||
                 l.Text == "Ürün Seçimi:" ||
                 l.Text == "Sebep Seçimi:" ||
                 l.Text == "Başlangıç Tarihi:" ||
                 l.Text == "Bitiş Tarihi:").ToList();
            
            foreach (var control in controlsToRemove)
            {
                this.Controls.Remove(control);
                control.Dispose();
            }

            // Filtre kontrollerini temizle
            if (cmbUrunFilter != null)
            {
                this.Controls.Remove(cmbUrunFilter);
                cmbUrunFilter.Dispose();
                cmbUrunFilter = null;
            }
            if (cmbSebepFilter != null)
            {
                this.Controls.Remove(cmbSebepFilter);
                cmbSebepFilter.Dispose();
                cmbSebepFilter = null;
            }
            if (dtpBaslangicFilter != null)
            {
                this.Controls.Remove(dtpBaslangicFilter);
                dtpBaslangicFilter.Dispose();
                dtpBaslangicFilter = null;
            }
            if (dtpBitisFilter != null)
            {
                this.Controls.Remove(dtpBitisFilter);
                dtpBitisFilter.Dispose();
                dtpBitisFilter = null;
            }
            if (btnFilter != null)
            {
                this.Controls.Remove(btnFilter);
                btnFilter.Dispose();
                btnFilter = null;
            }
                         if (btnClearFilter != null)
             {
                 this.Controls.Remove(btnClearFilter);
                 btnClearFilter.Dispose();
                 btnClearFilter = null;
             }

             // Dashboard kontrollerini temizle
             if (lblDashboardTitle != null)
             {
                 this.Controls.Remove(lblDashboardTitle);
                 lblDashboardTitle.Dispose();
                 lblDashboardTitle = null;
             }
             if (lblTotalTime != null)
             {
                 this.Controls.Remove(lblTotalTime);
                 lblTotalTime.Dispose();
                 lblTotalTime = null;
             }
             if (lblTotalTimeValue != null)
             {
                 this.Controls.Remove(lblTotalTimeValue);
                 lblTotalTimeValue.Dispose();
                 lblTotalTimeValue = null;
             }

            // Reset variables
            selectedUrunId = -1;
            selectedGecikmeId = -1;
            yeniKayitID = -1;
            isIncompleteTransactionSelected = false;
            
            // Timer'ları durdur
            if (timer != null)
            {
                timer.Stop();
            }
            if (statusTimer != null)
            {
                statusTimer.Stop();
            }

            // Admin seçimini sıfırla
            selectedAdminRecordId = -1;
        }

        private void ShowLoginScreen()
        {
            lblTitle.Text = "GECİKME TAKİP SİSTEMİ";
            lblSubtitle.Text = "RFID Kartınızı Tarayınız";

            // Yazıları tam ortaya konumlandır
            lblTitle.Location = new Point((this.ClientSize.Width - lblTitle.Width) / 2, (this.ClientSize.Height - lblTitle.Height - lblSubtitle.Height - 50) / 2);
            lblSubtitle.Location = new Point((this.ClientSize.Width - lblSubtitle.Width) / 2, lblTitle.Location.Y + lblTitle.Height + 30);

            // Admin değişkenlerini sıfırla
            isAdminUser = false;
            validatedKullaniciId = -1;

            // Sayaç kaldırıldı

            RFIDOkuyucuBaslat();
        }

        private void ShowMainMenuScreen()
        {
            lblTitle.Text = "ANA MENÜ";
            lblSubtitle.Text = $"Hoş Geldiniz - {GetUserName(validatedKullaniciId)}";

            // Ana menüde yazıları üst kısma konumlandır
            lblTitle.Location = new Point((this.ClientSize.Width - lblTitle.Width) / 2, 50);
            lblSubtitle.Location = new Point((this.ClientSize.Width - lblSubtitle.Width) / 2, 100);

            btnConfirm.Visible = true;
            btnConfirm.Text = "YENİ İŞLEM";

            btnIncomplete.Visible = true;
            btnIncomplete.Text = "TAMAMLANMAMIŞ İŞLEMLER";

            btnBack.Visible = true;
            btnBack.Text = "ÇIKIŞ";
        }

        private void ShowDualPanelScreen()
        {
            lblTitle.Text = "GECİKME TAKİP SİSTEMİ";
            lblSubtitle.Text = $"Hoş Geldiniz - {GetUserName(validatedKullaniciId)}";

            // Çift panel ekranında yazıları üst kısma konumlandır
            lblTitle.Location = new Point((this.ClientSize.Width - lblTitle.Width) / 2, 30);
            lblSubtitle.Location = new Point((this.ClientSize.Width - lblSubtitle.Width) / 2, 80);

            // Sol taraf başlığı - Ürünler
            Label lblProductsTitle = new Label
            {
                Text = "ÜRÜNLER",
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = Color.Black,
                AutoSize = true,
                Location = new Point(50, 150)
            };
            this.Controls.Add(lblProductsTitle);

            // Orta taraf başlığı - Sebepler
            Label lblReasonsTitle = new Label
            {
                Text = "SEBEPLER",
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = Color.Black,
                AutoSize = true,
                Location = new Point(50, 470)
            };
            this.Controls.Add(lblReasonsTitle);

            // Sağ taraf başlığı
            Label lblRightTitle = new Label
            {
                Text = "TAMAMLANMAMIŞ İŞLEMLER",
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = Color.Black,
                AutoSize = true,
                Location = new Point(700, 150)
            };
            this.Controls.Add(lblRightTitle);

            // Sayaç kaldırıldı

            // DataGridView'ları göster ve konumlandır
            dgvData.Visible = true;
            dgvData.Location = new Point(50, 180);
            dgvData.Size = new Size(600, 275);
            
            dgvReasons.Visible = true;
            dgvReasons.Location = new Point(50, 500);
            dgvReasons.Size = new Size(600, 275);
            
            dgvIncomplete.Visible = true;
            dgvIncomplete.Location = new Point(700, 180);
            dgvIncomplete.Size = new Size(600, 500);

            // Verileri yükle
            LoadProducts();
            LoadReasons();
            LoadIncompleteTransactions();
            
            // Timer'ı başlat
            timer.Start();

            // Kayıt oluştur butonu - Sol tarafın altında
            btnConfirm.Visible = true;
            btnConfirm.Text = "KAYIT OLUŞTUR";
            btnConfirm.Location = new Point(50, 795);
            btnConfirm.Size = new Size(200, 50);
            btnConfirm.BackColor = Color.Green;
            btnConfirm.ForeColor = Color.White;
            btnConfirm.Enabled = true;

            // Sağ taraf butonu - BİTİR
            btnIncomplete.Visible = true;
            btnIncomplete.Text = "BİTİR";
            btnIncomplete.Location = new Point(1000, 720);
            btnIncomplete.Size = new Size(150, 50);

            // Çıkış butonu - Sağ üst köşe
            btnBack.Visible = true;
            btnBack.Text = "ÇIKIŞ";
            btnBack.Location = new Point(1200, 30);
            btnBack.Size = new Size(150, 60);
            btnBack.BackColor = Color.Red;
            btnBack.ForeColor = Color.White;
        }

        private void ShowProductSelectionScreen()
        {
            lblTitle.Text = "ÜRÜN SEÇİMİ";
            lblSubtitle.Text = "Lütfen bir ürün seçiniz";

            // Ürün seçimi ekranında yazıları üst kısma konumlandır
            lblTitle.Location = new Point((this.ClientSize.Width - lblTitle.Width) / 2, 50);
            lblSubtitle.Location = new Point((this.ClientSize.Width - lblSubtitle.Width) / 2, 100);

            dgvData.Visible = true;
            LoadProducts();

            btnBack.Visible = true;
            btnBack.Text = "GERİ";

            btnCancel.Visible = true;
            btnCancel.Text = "İPTAL";
        }

        private void ShowDelayReasonScreen()
        {
            lblTitle.Text = "GECİKME NEDENİ";
            lblSubtitle.Text = $"Seçilen Ürün: {GetProductName(selectedUrunId)}";

            // Gecikme nedeni ekranında yazıları üst kısma konumlandır
            lblTitle.Location = new Point((this.ClientSize.Width - lblTitle.Width) / 2, 50);
            lblSubtitle.Location = new Point((this.ClientSize.Width - lblSubtitle.Width) / 2, 100);

            dgvData.Visible = true;
            LoadDelayReasons();

            btnBack.Visible = true;
            btnBack.Text = "GERİ";

            btnCancel.Visible = true;
            btnCancel.Text = "İPTAL";
        }

        private void ShowIncompleteTransactionsScreen()
        {
            lblTitle.Text = "TAMAMLANMAMIŞ İŞLEMLER";
            lblSubtitle.Text = "Lütfen tamamlanmamış işleminizi seçiniz";

            // Tamamlanmamış işlemler ekranında yazıları üst kısma konumlandır
            lblTitle.Location = new Point((this.ClientSize.Width - lblTitle.Width) / 2, 50);
            lblSubtitle.Location = new Point((this.ClientSize.Width - lblSubtitle.Width) / 2, 100);

            dgvIncomplete.Visible = true;
            LoadIncompleteTransactions();

            btnBack.Visible = true;
            btnBack.Text = "GERİ";

            btnCancel.Visible = true;
            btnCancel.Text = "İPTAL";
        }

        private void ShowConfirmationScreen()
        {
            lblTitle.Text = "ONAY EKRANI";
            lblSubtitle.Text = "Seçimlerinizi onaylayınız";

            // Onay ekranında yazıları üst kısma konumlandır
            lblTitle.Location = new Point((this.ClientSize.Width - lblTitle.Width) / 2, 50);
            lblSubtitle.Location = new Point((this.ClientSize.Width - lblSubtitle.Width) / 2, 100);

            lblTimer.Visible = true;
            StartTimer();

            btnConfirm.Visible = true;
            btnConfirm.Text = "TAMAMLA";

            btnCancel.Visible = true;
            btnCancel.Text = "İPTAL";
        }

        private void ShowAdminPanelScreen()
        {
            lblTitle.Text = "ADMIN PANELİ";
            lblSubtitle.Text = "Tüm Gecikme Kayıtları";

            // Admin panel ekranında yazıları üst kısma konumlandır
            lblTitle.Location = new Point((this.ClientSize.Width - lblTitle.Width) / 2, 30);
            lblSubtitle.Location = new Point((this.ClientSize.Width - lblSubtitle.Width) / 2, 80);

            // Sol taraf - Ana tablo
            dgvData.Visible = true;
            dgvData.Location = new Point(50, 150);
            dgvData.Size = new Size(800, 500);

            // Sağ taraf - Filtre paneli
            CreateFilterPanel();

            // Admin kayıtlarını yükle
            LoadAdminRecords();

            // Timer'ı başlat
            timer.Start();

            // Geri dönüş butonu
            btnBack.Visible = true;
            btnBack.Text = "GİRİŞ EKRANINA DÖN";
            btnBack.Location = new Point(50, 720);
            btnBack.Size = new Size(200, 50);

            // Aktif işlemleri bitirme butonu
            btnConfirm.Visible = true;
            btnConfirm.Text = "BİTİR";
            btnConfirm.Location = new Point(300, 720);
            btnConfirm.Size = new Size(200, 50);
            btnConfirm.BackColor = Color.Orange;
            btnConfirm.ForeColor = Color.White;
            btnConfirm.Enabled = false; // Başlangıçta devre dışı
        }

        private void CreateFilterPanel()
        {
            // Filtre başlığı
            Label lblFilterTitle = new Label
            {
                Text = "FİLTRE PANELİ",
                Font = new Font("Arial", 14, FontStyle.Bold),
                ForeColor = Color.Black,
                AutoSize = true,
                Location = new Point(900, 150)
            };
            this.Controls.Add(lblFilterTitle);

            // Ürün filtresi
            Label lblUrunFilter = new Label
            {
                Text = "Ürün Seçimi:",
                Font = new Font("Arial", 12, FontStyle.Regular),
                ForeColor = Color.Black,
                AutoSize = true,
                Location = new Point(900, 190)
            };
            this.Controls.Add(lblUrunFilter);

            cmbUrunFilter = new ComboBox
            {
                Location = new Point(900, 220),
                Size = new Size(200, 30),
                Font = new Font("Arial", 10, FontStyle.Regular),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            this.Controls.Add(cmbUrunFilter);

            // Sebep filtresi
            Label lblSebepFilter = new Label
            {
                Text = "Sebep Seçimi:",
                Font = new Font("Arial", 12, FontStyle.Regular),
                ForeColor = Color.Black,
                AutoSize = true,
                Location = new Point(900, 260)
            };
            this.Controls.Add(lblSebepFilter);

            cmbSebepFilter = new ComboBox
            {
                Location = new Point(900, 290),
                Size = new Size(200, 30),
                Font = new Font("Arial", 10, FontStyle.Regular),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            this.Controls.Add(cmbSebepFilter);

            // Tarih filtresi
            Label lblTarihFilter = new Label
            {
                Text = "Başlangıç Tarihi:",
                Font = new Font("Arial", 12, FontStyle.Regular),
                ForeColor = Color.Black,
                AutoSize = true,
                Location = new Point(900, 330)
            };
            this.Controls.Add(lblTarihFilter);

            dtpBaslangicFilter = new DateTimePicker
            {
                Location = new Point(900, 360),
                Size = new Size(200, 30),
                Font = new Font("Arial", 10, FontStyle.Regular),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };
            this.Controls.Add(dtpBaslangicFilter);

            // Bitiş tarihi filtresi
            Label lblBitisTarihFilter = new Label
            {
                Text = "Bitiş Tarihi:",
                Font = new Font("Arial", 12, FontStyle.Regular),
                ForeColor = Color.Black,
                AutoSize = true,
                Location = new Point(900, 400)
            };
            this.Controls.Add(lblBitisTarihFilter);

            dtpBitisFilter = new DateTimePicker
            {
                Location = new Point(900, 430),
                Size = new Size(200, 30),
                Font = new Font("Arial", 10, FontStyle.Regular),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };
            this.Controls.Add(dtpBitisFilter);

            // Filtre butonu
            btnFilter = new Button
            {
                Text = "FİLTRELE",
                Location = new Point(900, 480),
                Size = new Size(90, 35),
                Font = new Font("Arial", 10, FontStyle.Bold),
                BackColor = Color.LightBlue
            };
            btnFilter.Click += BtnFilter_Click;
            this.Controls.Add(btnFilter);

            // Temizle butonu
            btnClearFilter = new Button
            {
                Text = "TEMİZLE",
                Location = new Point(1010, 480),
                Size = new Size(90, 35),
                Font = new Font("Arial", 10, FontStyle.Bold),
                BackColor = Color.LightCoral
            };
            btnClearFilter.Click += BtnClearFilter_Click;
            this.Controls.Add(btnClearFilter);

            // Dashboard paneli
            CreateDashboardPanel();

            // Filtre kontrollerini yükle
            LoadFilterControls();
        }

        private void CreateDashboardPanel()
        {
            // Dashboard başlığı
            lblDashboardTitle = new Label
            {
                Text = "SÜRE",
                Font = new Font("Arial", 14, FontStyle.Bold),
                ForeColor = Color.Black,
                AutoSize = true,
                Location = new Point(1150, 150)
            };
            this.Controls.Add(lblDashboardTitle);

            // Toplam geçen süre başlığı
            lblTotalTime = new Label
            {
                Text = "Toplam Geçen Süre:",
                Font = new Font("Arial", 12, FontStyle.Bold),
                ForeColor = Color.Black,
                AutoSize = true,
                Location = new Point(1150, 190)
            };
            this.Controls.Add(lblTotalTime);

            // Toplam geçen süre değeri
            lblTotalTimeValue = new Label
            {
                Text = "00:00:00",
                Font = new Font("Arial", 16, FontStyle.Bold),
                ForeColor = Color.DarkBlue,
                AutoSize = true,
                Location = new Point(1150, 220)
            };
            this.Controls.Add(lblTotalTimeValue);
        }

        private void LoadFilterControls()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(CONNECTION_STRING))
                {
                    connection.Open();

                    // Ürünleri yükle
                    SqlCommand cmdUrunler = new SqlCommand("SELECT ID, Urunler FROM TblUrunler ORDER BY Urunler", connection);
                    SqlDataReader readerUrunler = cmdUrunler.ExecuteReader();
                    
                    cmbUrunFilter.Items.Clear();
                    cmbUrunFilter.Items.Add("Tümü");
                    cmbUrunFilter.SelectedIndex = 0;

                    while (readerUrunler.Read())
                    {
                        cmbUrunFilter.Items.Add(new ComboBoxItem
                        {
                            Text = readerUrunler["Urunler"].ToString(),
                            Value = Convert.ToInt32(readerUrunler["ID"])
                        });
                    }
                    readerUrunler.Close();

                    // Sebepleri yükle
                    SqlCommand cmdSebep = new SqlCommand("SELECT No, Nedenler FROM TblGecikmeNedenleri ORDER BY Nedenler", connection);
                    SqlDataReader readerSebep = cmdSebep.ExecuteReader();
                    
                    cmbSebepFilter.Items.Clear();
                    cmbSebepFilter.Items.Add("Tümü");
                    cmbSebepFilter.SelectedIndex = 0;

                    while (readerSebep.Read())
                    {
                        cmbSebepFilter.Items.Add(new ComboBoxItem
                        {
                            Text = readerSebep["Nedenler"].ToString(),
                            Value = Convert.ToInt32(readerSebep["No"])
                        });
                    }
                    readerSebep.Close();
                }
            }
            catch (Exception ex)
            {
                ShowStatusMessage($"Filtre kontrolleri yüklenirken hata: {ex.Message}", Color.Red);
            }
        }

        private void LoadProducts()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(CONNECTION_STRING))
                {
                    connection.Open();
                    SqlDataAdapter adapter = new SqlDataAdapter("SELECT ID, Urunler FROM TblUrunler", connection);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    
                    dgvData.Columns.Clear();
                    dgvData.Columns.Add("ID", "ID");
                    dgvData.Columns.Add("Urunler", "Ürün Adı");
                    dgvData.Columns["ID"].Visible = false;
                    
                    foreach (DataRow row in dt.Rows)
                    {
                        int rowIndex = dgvData.Rows.Add();
                        dgvData.Rows[rowIndex].Cells["ID"].Value = row["ID"];
                        dgvData.Rows[rowIndex].Cells["Urunler"].Value = row["Urunler"];
                    }
                }
            }
            catch (Exception ex)
            {
                ShowStatusMessage($"Ürünler yüklenirken hata: {ex.Message}", Color.Red);
            }
        }

        private void LoadDelayReasons()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(CONNECTION_STRING))
                {
                    connection.Open();
                    SqlDataAdapter adapter = new SqlDataAdapter("SELECT No, Nedenler FROM TblGecikmeNedenleri", connection);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    
                    dgvData.Columns.Clear();
                    dgvData.Columns.Add("No", "No");
                    dgvData.Columns.Add("Nedenler", "Gecikme Nedeni");
                    dgvData.Columns["No"].Visible = false;
                    
                    foreach (DataRow row in dt.Rows)
                    {
                        int rowIndex = dgvData.Rows.Add();
                        dgvData.Rows[rowIndex].Cells["No"].Value = row["No"];
                        dgvData.Rows[rowIndex].Cells["Nedenler"].Value = row["Nedenler"];
                    }
                }
            }
            catch (Exception ex)
            {
                ShowStatusMessage($"Gecikme nedenleri yüklenirken hata: {ex.Message}", Color.Red);
            }
        }

        private void LoadReasons()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(CONNECTION_STRING))
                {
                    connection.Open();
                    SqlDataAdapter adapter = new SqlDataAdapter("SELECT No, Nedenler FROM TblGecikmeNedenleri", connection);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    
                    dgvReasons.Columns.Clear();
                    dgvReasons.Columns.Add("No", "No");
                    dgvReasons.Columns.Add("Nedenler", "Gecikme Nedeni");
                    dgvReasons.Columns["No"].Visible = false;
                    
                    foreach (DataRow row in dt.Rows)
                    {
                        int rowIndex = dgvReasons.Rows.Add();
                        dgvReasons.Rows[rowIndex].Cells["No"].Value = row["No"];
                        dgvReasons.Rows[rowIndex].Cells["Nedenler"].Value = row["Nedenler"];
                    }
                }
            }
            catch (Exception ex)
            {
                ShowStatusMessage($"Gecikme nedenleri yüklenirken hata: {ex.Message}", Color.Red);
            }
        }

        private void LoadIncompleteTransactions()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(CONNECTION_STRING))
                {
                    connection.Open();
                    string query = @"
                        SELECT 
                            gk.ID,
                            gk.Baslangic,
                            u.Urunler,
                            gn.Nedenler
                        FROM TblGecikmeKaydı gk
                        INNER JOIN TblUrunler u ON gk.UrunID = u.ID
                        INNER JOIN TblGecikmeNedenleri gn ON gk.GecikmeID = gn.No
                        WHERE gk.KullaniciID = @kullaniciId 
                        AND gk.Baslangic IS NOT NULL 
                        AND gk.Bitis IS NULL
                        ORDER BY gk.Baslangic DESC";
                    
                    SqlCommand cmd = new SqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@kullaniciId", validatedKullaniciId);
                    
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    
                    dgvIncomplete.Columns.Clear();
                    dgvIncomplete.Columns.Add("ID", "ID");
                    dgvIncomplete.Columns.Add("GecenSure", "Geçen Süre");
                    dgvIncomplete.Columns.Add("Urunler", "Ürün");
                    dgvIncomplete.Columns.Add("Nedenler", "Gecikme Nedeni");
                    dgvIncomplete.Columns["ID"].Visible = false;

                    foreach (DataRow row in dt.Rows)
                    {
                        int rowIndex = dgvIncomplete.Rows.Add();
                        dgvIncomplete.Rows[rowIndex].Cells["ID"].Value = row["ID"];
                        
                        // Geçen süreyi hesapla
                        DateTime baslangic = Convert.ToDateTime(row["Baslangic"]);
                        TimeSpan gecenSure = DateTime.Now - baslangic;
                        string gecenSureText = $"{gecenSure.Hours:D2}:{gecenSure.Minutes:D2}:{gecenSure.Seconds:D2}";
                        
                        dgvIncomplete.Rows[rowIndex].Cells["GecenSure"].Value = gecenSureText;
                        dgvIncomplete.Rows[rowIndex].Cells["Urunler"].Value = row["Urunler"];
                        dgvIncomplete.Rows[rowIndex].Cells["Nedenler"].Value = row["Nedenler"];
                    }
                }
            }
            catch (Exception ex)
            {
                ShowStatusMessage($"Tamamlanmamış işlemler yüklenirken hata: {ex.Message}", Color.Red);
            }
        }

        private void UpdateIncompleteTransactionsTimer()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(CONNECTION_STRING))
                {
                    connection.Open();
                    string query = @"
                        SELECT 
                            gk.ID,
                            gk.Baslangic
                        FROM TblGecikmeKaydı gk
                        WHERE gk.KullaniciID = @kullaniciId 
                        AND gk.Baslangic IS NOT NULL 
                        AND gk.Bitis IS NULL
                        ORDER BY gk.Baslangic DESC";
                    
                    SqlCommand cmd = new SqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@kullaniciId", validatedKullaniciId);
                    
                    SqlDataReader reader = cmd.ExecuteReader();
                    int rowIndex = 0;
                    
                    while (reader.Read())
                    {
                        if (rowIndex < dgvIncomplete.Rows.Count)
                        {
                            DateTime baslangic = Convert.ToDateTime(reader["Baslangic"]);
                            TimeSpan gecenSure = DateTime.Now - baslangic;
                            string gecenSureText = $"{gecenSure.Hours:D2}:{gecenSure.Minutes:D2}:{gecenSure.Seconds:D2}";
                            
                            dgvIncomplete.Rows[rowIndex].Cells["GecenSure"].Value = gecenSureText;
                        }
                        rowIndex++;
                    }
                }
            }
            catch (Exception ex)
            {
                // Timer hatalarını sessizce geç
            }
        }

        private void LoadAdminRecords()
        {
            LoadAdminRecordsWithFilter(false);
        }

        private void LoadAdminRecordsWithFilter(bool applyFilters = true)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(CONNECTION_STRING))
                {
                    connection.Open();
                    
                    string baseQuery = @"
                        SELECT 
                            gk.ID,
                            gk.Baslangic,
                            gk.Bitis,
                            k.AdSoyad,
                            u.Urunler,
                            gn.Nedenler,
                            CASE 
                                WHEN gk.Bitis IS NULL THEN 'Aktif'
                                ELSE 'Tamamlandı'
                            END as Durum
                        FROM TblGecikmeKaydı gk
                        INNER JOIN Kullanıcılar k ON gk.KullaniciID = k.Id
                        INNER JOIN TblUrunler u ON gk.UrunID = u.ID
                        INNER JOIN TblGecikmeNedenleri gn ON gk.GecikmeID = gn.No
                        WHERE gk.Baslangic IS NOT NULL";

                    List<string> whereConditions = new List<string>();
                    List<SqlParameter> parameters = new List<SqlParameter>();

                    // Filtre uygulanıp uygulanmadığını kontrol et
                    bool hasFilter = false;

                    // Sadece filtreler uygulanacaksa filtreleri ekle
                    if (applyFilters)
                    {
                        // Ürün filtresi
                        if (cmbUrunFilter.SelectedIndex > 0 && cmbUrunFilter.SelectedItem is ComboBoxItem selectedUrun)
                        {
                            whereConditions.Add("gk.UrunID = @urunId");
                            parameters.Add(new SqlParameter("@urunId", selectedUrun.Value));
                            hasFilter = true;
                        }

                        // Sebep filtresi
                        if (cmbSebepFilter.SelectedIndex > 0 && cmbSebepFilter.SelectedItem is ComboBoxItem selectedSebep)
                        {
                            whereConditions.Add("gk.GecikmeID = @sebepId");
                            parameters.Add(new SqlParameter("@sebepId", selectedSebep.Value));
                            hasFilter = true;
                        }

                        // Başlangıç tarihi filtresi
                        whereConditions.Add("CAST(gk.Baslangic AS DATE) >= @baslangicTarih");
                        parameters.Add(new SqlParameter("@baslangicTarih", dtpBaslangicFilter.Value.Date));
                        hasFilter = true;

                        // Bitiş tarihi filtresi
                        whereConditions.Add("CAST(gk.Bitis AS DATE) <= @bitisTarih");
                        parameters.Add(new SqlParameter("@bitisTarih", dtpBitisFilter.Value.Date));
                        hasFilter = true;

                        // Filtre varsa sadece tamamlanmış kayıtları getir
                        if (hasFilter)
                        {
                            whereConditions.Add("gk.Bitis IS NOT NULL");
                        }
                    }

                    // WHERE koşullarını ekle
                    if (whereConditions.Count > 0)
                    {
                        baseQuery += " AND " + string.Join(" AND ", whereConditions);
                    }

                    baseQuery += " ORDER BY gk.Baslangic DESC";

                    SqlCommand cmd = new SqlCommand(baseQuery, connection);
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.Add(param);
                    }

                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    dgvData.Columns.Clear();
                    dgvData.Columns.Add("ID", "ID");
                    dgvData.Columns.Add("Kullanici", "Kullanıcı");
                    dgvData.Columns.Add("Baslangic", "Başlangıç Zamanı");
                    dgvData.Columns.Add("Bitis", "Bitiş Zamanı");
                    dgvData.Columns.Add("GecenSure", "Geçen Süre");
                    dgvData.Columns.Add("Urunler", "Ürün");
                    dgvData.Columns.Add("Nedenler", "Gecikme Nedeni");
                    dgvData.Columns.Add("Durum", "Durum");
                    dgvData.Columns["ID"].Visible = false;

                    // Toplam geçen süreyi hesapla
                    int totalSeconds = 0;
                     
                    foreach (DataRow row in dt.Rows)
                    {
                        int rowIndex = dgvData.Rows.Add();
                        dgvData.Rows[rowIndex].Cells["ID"].Value = row["ID"];
                        dgvData.Rows[rowIndex].Cells["Kullanici"].Value = row["AdSoyad"];
                        dgvData.Rows[rowIndex].Cells["Baslangic"].Value = Convert.ToDateTime(row["Baslangic"]).ToString("dd.MM.yyyy HH:mm:ss");
                         
                        if (row["Bitis"] != DBNull.Value)
                        {
                            dgvData.Rows[rowIndex].Cells["Bitis"].Value = Convert.ToDateTime(row["Bitis"]).ToString("dd.MM.yyyy HH:mm:ss");
                        }
                        else
                        {
                            dgvData.Rows[rowIndex].Cells["Bitis"].Value = "-";
                        }
                         
                        // Geçen süreyi hesapla
                        if (row["Durum"].ToString() == "Aktif")
                        {
                            // Aktif kayıtlar için geçen süreyi hesapla
                            DateTime baslangic = Convert.ToDateTime(row["Baslangic"]);
                            TimeSpan gecenSure = DateTime.Now - baslangic;
                            string gecenSureText = $"{gecenSure.Hours:D2}:{gecenSure.Minutes:D2}:{gecenSure.Seconds:D2}";
                            dgvData.Rows[rowIndex].Cells["GecenSure"].Value = gecenSureText;
                            
                            // Aktif kayıtları yeşil yap
                            dgvData.Rows[rowIndex].DefaultCellStyle.BackColor = Color.LightGreen;
                        }
                        else
                        {
                            // Tamamlanmış kayıtlar için bitiş zamanından hesapla
                            DateTime baslangic = Convert.ToDateTime(row["Baslangic"]);
                            DateTime bitis = Convert.ToDateTime(row["Bitis"]);
                            TimeSpan gecenSure = bitis - baslangic;
                            string gecenSureText = $"{gecenSure.Hours:D2}:{gecenSure.Minutes:D2}:{gecenSure.Seconds:D2}";
                            dgvData.Rows[rowIndex].Cells["GecenSure"].Value = gecenSureText;
                            
                            // Toplam geçen süreyi hesapla (sadece tamamlanmış kayıtlar)
                            totalSeconds += (int)gecenSure.TotalSeconds;
                        }
                        
                        dgvData.Rows[rowIndex].Cells["Urunler"].Value = row["Urunler"];
                        dgvData.Rows[rowIndex].Cells["Nedenler"].Value = row["Nedenler"];
                        dgvData.Rows[rowIndex].Cells["Durum"].Value = row["Durum"];
                    }
                     
                    // Dashboard'da toplam süreyi göster
                    UpdateDashboardTotalTime(totalSeconds);
                }
            }
            catch (Exception ex)
            {
                ShowStatusMessage($"Admin kayıtları yüklenirken hata: {ex.Message}", Color.Red);
            }
        }

         private void UpdateDashboardTotalTime(int totalSeconds)
         {
             if (lblTotalTimeValue != null)
             {
                 TimeSpan totalTime = TimeSpan.FromSeconds(totalSeconds);
                 lblTotalTimeValue.Text = $"{totalTime.Hours:D2}:{totalTime.Minutes:D2}:{totalTime.Seconds:D2}";
             }
         }

         private void UpdateAdminRecordsTimer()
         {
             try
             {
                 using (SqlConnection connection = new SqlConnection(CONNECTION_STRING))
                 {
                     connection.Open();
                     string query = @"
                         SELECT 
                             gk.ID,
                             gk.Baslangic
                         FROM TblGecikmeKaydı gk
                         WHERE gk.Baslangic IS NOT NULL 
                         AND gk.Bitis IS NULL
                         ORDER BY gk.Baslangic DESC";
                     
                     SqlCommand cmd = new SqlCommand(query, connection);
                     SqlDataReader reader = cmd.ExecuteReader();
                     int rowIndex = 0;
                     
                     while (reader.Read())
                     {
                         // Admin tablosunda aktif kayıtları bul ve güncelle
                         for (int i = 0; i < dgvData.Rows.Count; i++)
                         {
                             if (dgvData.Rows[i].Cells["ID"].Value.ToString() == reader["ID"].ToString())
                             {
                                 DateTime baslangic = Convert.ToDateTime(reader["Baslangic"]);
                                 TimeSpan gecenSure = DateTime.Now - baslangic;
                                 string gecenSureText = $"{gecenSure.Hours:D2}:{gecenSure.Minutes:D2}:{gecenSure.Seconds:D2}";
                                 
                                 dgvData.Rows[i].Cells["GecenSure"].Value = gecenSureText;
                                 break;
                             }
                         }
                     }
                 }
             }
             catch (Exception ex)
             {
                 // Timer hatalarını sessizce geç
             }
         }

        private void DgvData_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = dgvData.Rows[e.RowIndex];

                switch (currentScreen)
                {
                    case AppScreen.AdminPanel:
                        // Admin panelinde kayıt seçildi
                        selectedAdminRecordId = Convert.ToInt32(row.Cells["ID"].Value);
                        string durum = row.Cells["Durum"].Value.ToString();
                        
                        // Seçilen kayıt aktif mi kontrol et
                        if (durum == "Aktif")
                        {
                            // Seçilen kayıtı vurgula
                            dgvData.ClearSelection();
                            dgvData.Rows[e.RowIndex].Selected = true;
                            
                            // BİTİR butonunu aktif et
                            btnConfirm.Enabled = true;
                            
                            ShowStatusMessage($"Seçilen aktif kayıt: {row.Cells["Kullanici"].Value} - {row.Cells["Urunler"].Value}", Color.Green);
                        }
                        else
                        {
                            // Tamamlanmış kayıt seçildi, butonu devre dışı bırak
                            btnConfirm.Enabled = false;
                            ShowStatusMessage("Sadece aktif kayıtlar bitirilebilir!", Color.Orange);
                        }
                        break;

                    case AppScreen.DualPanel:
                        // Çift panel ekranında ürün seçildi
                        selectedUrunId = Convert.ToInt32(row.Cells["ID"].Value);
                        
                        // Seçilen ürünü vurgula
                        dgvData.ClearSelection();
                        dgvData.Rows[e.RowIndex].Selected = true;
                        
                        ShowStatusMessage($"Seçilen ürün: {row.Cells["Urunler"].Value}", Color.Green);
                        break;

                    case AppScreen.ProductSelection:
                        selectedUrunId = Convert.ToInt32(row.Cells["ID"].Value);
                        ShowScreen(AppScreen.DelayReason);
                        break;

                    case AppScreen.DelayReason:
                        selectedGecikmeId = Convert.ToInt32(row.Cells["No"].Value);
                        CreateNewRecord();
                        ShowScreen(AppScreen.Confirmation);
                        break;

                    case AppScreen.IncompleteTransactions:
                        yeniKayitID = Convert.ToInt32(row.Cells["ID"].Value);
                        isIncompleteTransactionSelected = true;
                        LoadTransactionDetails(yeniKayitID);
                        ShowScreen(AppScreen.Confirmation);
                        break;
                }
            }
        }

        private void DgvIncomplete_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = dgvIncomplete.Rows[e.RowIndex];

                switch (currentScreen)
                {
                    case AppScreen.IncompleteTransactions:
                        yeniKayitID = Convert.ToInt32(row.Cells["ID"].Value);
                        isIncompleteTransactionSelected = true;
                        LoadTransactionDetails(yeniKayitID);
                        ShowScreen(AppScreen.Confirmation);
                break;
                }
            }
        }

        private void DgvReasons_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = dgvReasons.Rows[e.RowIndex];
                selectedGecikmeId = Convert.ToInt32(row.Cells["No"].Value);
                
                // Seçilen sebebi vurgula
                dgvReasons.ClearSelection();
                dgvReasons.Rows[e.RowIndex].Selected = true;
                
                ShowStatusMessage($"Seçilen sebep: {row.Cells["Nedenler"].Value}", Color.Green);
            }
        }

        private void CreateNewRecord()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(CONNECTION_STRING))
                {
                    connection.Open();
                    SqlCommand cmd = new SqlCommand(
                        "INSERT INTO TblGecikmeKaydı (KullaniciID, UrunID, GecikmeID, Baslangic) VALUES (@kullanici, @urun, @gecikme, @baslangic); SELECT SCOPE_IDENTITY();",
                        connection);
                    
                    cmd.Parameters.AddWithValue("@kullanici", validatedKullaniciId);
                    cmd.Parameters.AddWithValue("@urun", selectedUrunId);
                    cmd.Parameters.AddWithValue("@gecikme", selectedGecikmeId);
                    cmd.Parameters.AddWithValue("@baslangic", DateTime.Now);

                    yeniKayitID = Convert.ToInt32(cmd.ExecuteScalar());
                    ShowStatusMessage("Yeni kayıt oluşturuldu!", Color.Green);
                }
            }
            catch (Exception ex)
            {
                ShowStatusMessage($"Kayıt oluşturulurken hata: {ex.Message}", Color.Red);
            }
        }

        private void LoadTransactionDetails(int recordId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(CONNECTION_STRING))
                {
                    connection.Open();
                    SqlCommand cmd = new SqlCommand(
                        "SELECT KullaniciID, UrunID, GecikmeID, Baslangic FROM TblGecikmeKaydı WHERE ID = @recordId",
                        connection);

                    cmd.Parameters.AddWithValue("@recordId", recordId);
                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        validatedKullaniciId = Convert.ToInt32(reader["KullaniciID"]);
                        selectedUrunId = Convert.ToInt32(reader["UrunID"]);
                        selectedGecikmeId = Convert.ToInt32(reader["GecikmeID"]);
                        baslangicZamani = Convert.ToDateTime(reader["Baslangic"]);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowStatusMessage($"İşlem detayları yüklenirken hata: {ex.Message}", Color.Red);
            }
        }

        private void StartTimer()
        {
            saniye = 0;
            dakika = 0;
            saat = 0;
            lblTimer.Text = "00:00:00";

            if (timer != null)
            {
                timer.Stop();
                timer.Dispose();
            }

            timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000;
            timer.Tick += (s, args) =>
            {
                saniye++;
                if (saniye == 60)
                {
                    saniye = 0;
                    dakika++;
                }
                if (dakika == 60)
                {
                    dakika = 0;
                    saat++;
                }
                lblTimer.Text = $"{saat:D2}:{dakika:D2}:{saniye:D2}";
            };
            timer.Start();
        }

        private void StopTimer()
        {
            if (timer != null && timer.Enabled)
            {
                timer.Stop();
            }
        }

        private void CompleteSelectedIncompleteTransaction()
        {
            if (dgvIncomplete.SelectedRows.Count == 0)
            {
                ShowStatusMessage("Lütfen tamamlanacak işlemi seçiniz!", Color.Red);
                return;
            }

            int selectedRecordId = Convert.ToInt32(dgvIncomplete.SelectedRows[0].Cells["ID"].Value);

            try
            {
                using (SqlConnection connection = new SqlConnection(CONNECTION_STRING))
                {
                    connection.Open();
                    SqlCommand cmd = new SqlCommand(
                        "UPDATE TblGecikmeKaydı SET Bitis = @bitis WHERE ID = @recordId",
                        connection);
                    
                    cmd.Parameters.AddWithValue("@bitis", DateTime.Now);
                    cmd.Parameters.AddWithValue("@recordId", selectedRecordId);
                    
                    int affectedRows = cmd.ExecuteNonQuery();
                    
                    if (affectedRows > 0)
                    {
                        ShowStatusMessage("İşlem başarıyla tamamlandı!", Color.Green);
                        LoadIncompleteTransactions(); // Listeyi yenile
                    }
                    else
                    {
                        ShowStatusMessage("İşlem tamamlanamadı!", Color.Red);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowStatusMessage($"İşlem tamamlanırken hata: {ex.Message}", Color.Red);
            }
        }

        private void CompleteRecord()
        {
            StopTimer();

            try
            {
                using (SqlConnection connection = new SqlConnection(CONNECTION_STRING))
                {
                    connection.Open();
                    SqlCommand cmd = new SqlCommand(
                        "UPDATE TblGecikmeKaydı SET Bitis = @bitis WHERE ID = @recordId",
                        connection);
                    
                    cmd.Parameters.AddWithValue("@bitis", DateTime.Now);
                    cmd.Parameters.AddWithValue("@recordId", yeniKayitID);
                    
                    int affectedRows = cmd.ExecuteNonQuery();
                    
                    if (affectedRows > 0)
                    {
                        ShowStatusMessage("İşlem başarıyla tamamlandı!", Color.Green);
                        ShowScreen(AppScreen.MainMenu);
                    }
                    else
                    {
                        ShowStatusMessage("İşlem tamamlanamadı!", Color.Red);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowStatusMessage($"İşlem tamamlanırken hata: {ex.Message}", Color.Red);
            }
        }

        private void CompleteAdminRecord(int recordId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(CONNECTION_STRING))
                {
                    connection.Open();
                    SqlCommand cmd = new SqlCommand(
                        "UPDATE TblGecikmeKaydı SET Bitis = @bitis WHERE ID = @recordId",
                        connection);
                    
                    cmd.Parameters.AddWithValue("@bitis", DateTime.Now);
                    cmd.Parameters.AddWithValue("@recordId", recordId);
                    
                    int affectedRows = cmd.ExecuteNonQuery();
                    
                    if (affectedRows > 0)
                    {
                        ShowStatusMessage("Admin kayıt başarıyla tamamlandı!", Color.Green);
                        
                        // Seçimi sıfırla
                        selectedAdminRecordId = -1;
                        btnConfirm.Enabled = false;
                        
                        // Tabloyu yeniden yükle
                        LoadAdminRecords();
                    }
                    else
                    {
                        ShowStatusMessage("Admin kayıt tamamlanamadı!", Color.Red);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowStatusMessage($"Admin kayıt tamamlanırken hata: {ex.Message}", Color.Red);
            }
        }

        private string GetUserName(int userId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(CONNECTION_STRING))
                {
                    connection.Open();
                    SqlCommand cmd = new SqlCommand("SELECT AdSoyad FROM Kullanıcılar WHERE Id = @userId", connection);
                    cmd.Parameters.AddWithValue("@userId", userId);

                    object result = cmd.ExecuteScalar();
                    return result?.ToString() ?? "Bilinmeyen Kullanıcı";
                }
            }
            catch
            {
                return "Bilinmeyen Kullanıcı";
            }
        }

        private string GetProductName(int productId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(CONNECTION_STRING))
                {
                    connection.Open();
                    SqlCommand cmd = new SqlCommand("SELECT Urunler FROM TblUrunler WHERE ID = @productId", connection);
                    cmd.Parameters.AddWithValue("@productId", productId);

                    object result = cmd.ExecuteScalar();
                    return result?.ToString() ?? "Bilinmeyen Ürün";
                }
            }
            catch
            {
                return "Bilinmeyen Ürün";
            }
        }

        // Event Handlers
        private void BtnBack_Click(object sender, EventArgs e)
        {
            switch (currentScreen)
            {
                case AppScreen.Login:
                    this.Close();
                    break;
                case AppScreen.MainMenu:
                    ShowScreen(AppScreen.Login);
                    break;
                case AppScreen.DualPanel:
                    ShowScreen(AppScreen.Login);
                    break;
                case AppScreen.AdminPanel:
                    ShowScreen(AppScreen.Login);
                    break;
                case AppScreen.ProductSelection:
                case AppScreen.DelayReason:
                case AppScreen.IncompleteTransactions:
                    ShowScreen(AppScreen.MainMenu);
                    break;
                case AppScreen.Confirmation:
                    ShowScreen(AppScreen.MainMenu);
                    break;
            }
        }

        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            switch (currentScreen)
            {
                case AppScreen.MainMenu:
                    ShowScreen(AppScreen.DualPanel);
                    break;
                case AppScreen.DualPanel:
                    // KAYIT OLUŞTUR butonuna basıldığında
                    ShowStatusMessage("Buton tıklandı!", Color.Blue);
                    
                    if (selectedUrunId == -1)
                    {
                        ShowStatusMessage("Lütfen önce bir ürün seçiniz!", Color.Red);
                        return;
                    }
                    if (selectedGecikmeId == -1)
                    {
                        ShowStatusMessage("Lütfen önce bir sebep seçiniz!", Color.Red);
                        return;
                    }
                    
                    // Kayıt oluştur
                    CreateNewRecord();
                    ShowStatusMessage("Kayıt oluşturuldu!", Color.Green);
                    
                    // Seçimleri sıfırla
                    selectedUrunId = -1;
                    selectedGecikmeId = -1;
                    
                    // Seçimleri temizle
                    dgvData.ClearSelection();
                    dgvReasons.ClearSelection();
                    
                    // Tamamlanmamış işlemleri yeniden yükle (yeni kayıt eklendiği için)
                    LoadIncompleteTransactions();
                    break;
                case AppScreen.Confirmation:
                    CompleteRecord();
                    break;
                case AppScreen.AdminPanel:
                    // Admin panelinde BİTİR butonuna basıldığında
                    if (selectedAdminRecordId == -1)
                    {
                        ShowStatusMessage("Lütfen önce bir aktif kayıt seçiniz!", Color.Red);
                        return;
                    }
                    
                    // Seçilen kaydı bitir
                    CompleteAdminRecord(selectedAdminRecordId);
                    break;
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            switch (currentScreen)
            {
                case AppScreen.Login:
                    this.Close();
                    break;
                case AppScreen.DualPanel:
                    ShowScreen(AppScreen.Login);
                    break;
                case AppScreen.ProductSelection:
                case AppScreen.DelayReason:
                case AppScreen.IncompleteTransactions:
                    ShowScreen(AppScreen.MainMenu);
                    break;
                case AppScreen.Confirmation:
                    ShowScreen(AppScreen.MainMenu);
                    break;
            }
        }

        private void BtnIncomplete_Click(object sender, EventArgs e)
        {
            switch (currentScreen)
            {
                case AppScreen.MainMenu:
                    ShowScreen(AppScreen.IncompleteTransactions);
                    break;
                case AppScreen.DualPanel:
                    CompleteSelectedIncompleteTransaction();
                    break;
            }
        }

        private void BtnFilter_Click(object sender, EventArgs e)
        {
            LoadAdminRecordsWithFilter(true);
        }

        private void BtnClearFilter_Click(object sender, EventArgs e)
        {
            // Filtreleri temizle
            cmbUrunFilter.SelectedIndex = 0;
            cmbSebepFilter.SelectedIndex = 0;
            dtpBaslangicFilter.Value = DateTime.Today;
            dtpBitisFilter.Value = DateTime.Today;

            // Tüm kayıtları yükle
            LoadAdminRecords();
        }

        // RFID Methods
        private void ImpinjOkuyucuBaslat()
        {
            reader.TagsReported += EtiketlerRaporlandi;
            reader.ConnectionLost += BaglantiKesildi;
        }

        private void RFIDOkuyucuBaslat()
        {
            try
            {
                if (!reader.IsConnected)
                {
                    reader.Connect(READER_IP_ADDRESS);
                    Settings settings = reader.QueryDefaultSettings();
                    settings.Report.IncludeAntennaPortNumber = true;
                    settings.Report.IncludePeakRssi = true;
                    settings.Report.IncludeLastSeenTime = true;
                    settings.Report.IncludeSeenCount = true;
                    settings.Antennas.EnableAll();
                    settings.Antennas.TxPowerInDbm = 15.0;
                    settings.Antennas.RxSensitivityInDbm = -30.0;
                    reader.ApplySettings(settings);
                }

                if (!isReading)
                {
                    reader.Start();
                    isReading = true;
                }
            }
            catch (Exception ex)
            {
                ShowStatusMessage($"RFID Okuyucu Hatası: {ex.Message}", Color.Red);
            }
        }

        private void EtiketlerRaporlandi(ImpinjReader sender, TagReport report)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(() => EtiketlerRaporlandi(sender, report)));
                return;
            }

            foreach (Tag tag in report.Tags)
            {
                string rfidTag = tag.Epc.ToString();

                if (currentScreen == AppScreen.Login)
                {
                    reader.Stop();
                    isReading = false;
                    ValidateUserAndProceed(rfidTag);
                }
                break;
            }
        }

        private void ValidateUserAndProceed(string rfidTag)
        {
            try
            {
                // Admin kontrolü
                if (rfidTag == ADMIN_RFID_TAG)
                {
                    isAdminUser = true;
                    validatedKullaniciId = -1;
                    ShowStatusMessage("Admin girişi başarılı!", Color.Green);
                    ShowScreen(AppScreen.AdminPanel);
                    return;
                }

                using (SqlConnection connection = new SqlConnection(CONNECTION_STRING))
                {
                    connection.Open();
                    SqlCommand cmd = new SqlCommand("SELECT Id, AdSoyad FROM Kullanıcılar WHERE RFID = @tag", connection);
                    cmd.Parameters.AddWithValue("@tag", rfidTag);

                    SqlDataReader dbReader = cmd.ExecuteReader();
                    if (dbReader.Read())
                    {
                        validatedKullaniciId = (int)dbReader["Id"];
                        isAdminUser = false;
                        ShowStatusMessage("Giriş başarılı!", Color.Green);
                        ShowScreen(AppScreen.DualPanel);
                    }
                    else
                    {
                        ShowStatusMessage("Kart tanınmadı! Lütfen geçerli bir kart kullanın.", Color.Red);
                        ShowScreen(AppScreen.Login);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowStatusMessage($"Kullanıcı doğrulama hatası: {ex.Message}", Color.Red);
                ShowScreen(AppScreen.Login);
            }
        }

        private void BaglantiKesildi(ImpinjReader sender)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(() => BaglantiKesildi(sender)));
                return;
            }
            ShowStatusMessage("RFID okuyucu bağlantısı kesildi!", Color.Red);
        }

        private void VeritabaniBaglantiTestEt()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(CONNECTION_STRING))
                {
                    connection.Open();
                    ShowStatusMessage("Veritabanı bağlantısı başarılı!", Color.Green);
                }
            }
            catch (Exception ex)
            {
                ShowStatusMessage($"Veritabanı test hatası: {ex.Message}", Color.Red);
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            // Sadece giriş ekranındayken yazıları yeniden konumlandır
            if (currentScreen == AppScreen.Login)
            {
                lblTitle.Location = new Point((this.ClientSize.Width - lblTitle.Width) / 2, (this.ClientSize.Height - lblTitle.Height - lblSubtitle.Height - 50) / 2);
                lblSubtitle.Location = new Point((this.ClientSize.Width - lblSubtitle.Width) / 2, lblTitle.Location.Y + lblTitle.Height + 30);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopTimer();
            if (timer != null)
            {
                timer.Dispose();
            }
            
            if (statusTimer != null)
            {
                statusTimer.Dispose();
            }
            
            if (reader.IsConnected)
            {
                try
                {
                    if (isReading)
                    {
                        reader.Stop();
                    }
                    reader.Disconnect();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Kapatma sırasında hata: {ex.Message}");
                }
            }
            reader.TagsReported -= EtiketlerRaporlandi;
            reader.ConnectionLost -= BaglantiKesildi;

            base.OnFormClosing(e);
        }
    }
}
