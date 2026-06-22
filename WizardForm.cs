
using Microsoft.Data.SqlClient;
using System.Data;
using System.Globalization;
using System.Drawing.Drawing2D;
using System.Text;

namespace ALNOUR.InvoiceManager;

public sealed class InvoiceOption
{
    public long S_ID { get; init; }
    public string Display { get; init; } = "";
    public override string ToString() => Display;
}

public sealed class ItemOption
{
    public long CLS_ID { get; init; }
    public long SP_S_ID { get; init; }
    public string Name { get; init; } = "";
    public decimal OldPrice { get; init; }
    public decimal Qty { get; init; }
    public string Display => $"{Name} | السعر الحالي {OldPrice:0.##} | الكمية {Qty:0.##}";
    public override string ToString() => Display;
}

public sealed class WizardForm : Form
{
    private int _step = 0;
    private readonly Panel _content = new();
    private readonly Button _back = new();
    private readonly Button _next = new();
    private readonly Button _syncBtn = new();
    private readonly Button _previewBtn = new();
    private readonly Button _executeBtn = new();
    private readonly Label _title = new();
    private readonly Label _subtitle = new();
    private readonly PictureBox _logo = new();
    private readonly ProgressBar _progress = new();

    private readonly TextBox _server = new();
    private readonly TextBox _database = new();
    private readonly ComboBox _invoiceBox = new();
    private readonly ComboBox _itemBox = new();
    private readonly ComboBox _priceBox = new();
    private readonly CheckBox _payEqualsTotal = new();
    private readonly TextBox _qtyBox = new();

    private readonly DataGridView _grid = new();
    private readonly TextBox _log = new();
    private readonly CheckBox _confirm = new();

    private DataTable? _previewTable;

    private InvoiceOption? SelectedInvoiceOption => _invoiceBox.SelectedItem as InvoiceOption;
    private ItemOption? SelectedItemOption => _itemBox.SelectedItem as ItemOption;

    private long? SelectedInvoice
    {
        get
        {
            if (SelectedInvoiceOption != null) return SelectedInvoiceOption.S_ID;
            return long.TryParse((_invoiceBox.Text ?? "").Trim(), out var v) ? v : null;
        }
    }

    private decimal? SelectedPrice => decimal.TryParse((_priceBox.Text ?? "").Trim(), out var v) ? v : null;
    private decimal SelectedQty
    {
        get
        {
            var s = (_qtyBox.Text ?? "").Trim().Replace(",", ".");
            return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0M;
        }
    }

    public WizardForm()
    {
        Text = "POS Data Editor";
        MinimumSize = new Size(1000, 650);
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = true;
        Font = new Font("Segoe UI", 10);
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;
        BackColor = Color.FromArgb(232, 244, 255);
        BuildShell();
        RenderStep();
    }

    private void BuildShell()
    {
        var header = new Panel { Dock = DockStyle.Top, Height = 130, Padding = new Padding(24), BackColor = Color.FromArgb(214, 235, 252) };

        _title.Text = "مدير بيانات نقاط البيع";
        _title.Font = new Font("Segoe UI", 24, FontStyle.Bold);
        _title.ForeColor = Color.FromArgb(0, 115, 210);
        _title.Dock = DockStyle.Top;
        _title.TextAlign = ContentAlignment.MiddleRight;
        _title.Height = 44;

        _subtitle.Text = "";
        _subtitle.Visible = false;
        _subtitle.Dock = DockStyle.Top;
        _subtitle.Height = 0;

        _progress.Dock = DockStyle.Bottom;
        _progress.Minimum = 0;
        _progress.Maximum = 4;

        header.Controls.Add(_progress);
        header.Controls.Add(_subtitle);
        header.Controls.Add(_title);

        _content.Dock = DockStyle.Fill;
        _content.Padding = new Padding(24);
        _content.BackColor = Color.FromArgb(232, 244, 255);

        var footer = new Panel { Dock = DockStyle.Bottom, Height = 86, Padding = new Padding(24, 16, 24, 16), BackColor = Color.FromArgb(214, 235, 252) };

        _next.Text = "التالي";
        _back.Text = "السابق";
        _syncBtn.Text = "مزامنة";
        _previewBtn.Text = "معاينة";
        _executeBtn.Text = "تنفيذ التعديل";

        foreach (var b in new[] { _next, _back, _syncBtn, _previewBtn, _executeBtn })
        {
            b.Width = b == _executeBtn ? 160 : 135;
            b.Height = 44;
            StyleButton(b, b != _back);
        }

        _back.Click += (_, _) => { if (_step > 0) { _step--; RenderStep(); } };
        _next.Click += (_, _) => { if (_step < 3) { _step++; RenderStep(); } };
        _syncBtn.Click += async (_, _) => await SyncCurrentStepAsync();
        _previewBtn.Click += async (_, _) => await PreviewAsync();
        _executeBtn.Click += async (_, _) => await ExecuteAsync();

        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        flow.Controls.Add(_next);
        flow.Controls.Add(_back);
        flow.Controls.Add(_syncBtn);
        flow.Controls.Add(_previewBtn);
        flow.Controls.Add(_executeBtn);
        footer.Controls.Add(flow);

        Controls.Add(_content);
        Controls.Add(footer);
        Controls.Add(header);

        var footerSignature = new Label
        {
            Text = "Haitham Osama Abdelghaffar | iPharmEGY",
            Dock = DockStyle.Bottom,
            Height = 24,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(0, 75, 145),
            Padding = new Padding(12, 0, 0, 0)
        };
        Controls.Add(footerSignature);
        ConfigureBrandLogo();

        Resize += (_, _) =>
        {
            _logo.Left = 24;
            _logo.Top = 22;
        };
    }

    private static void AutoCloseSuccess(string message, int milliseconds = 1800)
    {
        using var form = new Form
        {
            Text = "POS Data Editor",
            Width = 520,
            Height = 260,
            StartPosition = FormStartPosition.CenterScreen,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            TopMost = true,
            RightToLeft = RightToLeft.Yes,
            RightToLeftLayout = true
        };

        var label = new Label
        {
            Text = message,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 12, FontStyle.Bold)
        };

        form.Controls.Add(label);

        using var timer = new System.Windows.Forms.Timer { Interval = milliseconds };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            form.DialogResult = DialogResult.OK;
            form.Close();
        };

        form.Shown += (_, _) => timer.Start();
        form.ShowDialog();
    }

    private void ConfigureBrandLogo()
    {
        _logo.Width = 44;
        _logo.Height = 44;
        _logo.SizeMode = PictureBoxSizeMode.Zoom;
        _logo.BackColor = Color.Transparent;
        _logo.BorderStyle = BorderStyle.None;

        var logoPath = Path.Combine(Application.StartupPath, "logo_app.png");
        if (!File.Exists(logoPath))
            logoPath = Path.Combine(Application.StartupPath, "logo_default.png");

        if (File.Exists(logoPath))
            _logo.Image = Image.FromFile(logoPath);

        _logo.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        _logo.Left = 24;
        _logo.Top = 22;

        Controls.Add(_logo);
        _logo.BringToFront();
    }

    private static void StyleButton(Button b, bool accent)
    {
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 1;
        b.FlatAppearance.BorderColor = accent ? Color.FromArgb(0, 115, 210) : Color.FromArgb(90, 120, 145);
        b.BackColor = accent ? Color.FromArgb(0, 145, 230) : Color.FromArgb(55, 125, 210);
        b.ForeColor = Color.White;
        b.Margin = new Padding(8, 0, 0, 0);
        b.Cursor = Cursors.Hand;
    }

    private void RenderStep()
    {
        _content.Controls.Clear();
        _progress.Value = _step + 1;
        _back.Enabled = _step > 0;
        _next.Visible = _step < 3;
        _syncBtn.Visible = _step == 0 || _step == 1;
        _previewBtn.Visible = _step == 2;
        _executeBtn.Visible = _step == 3;

        switch (_step)
        {
            case 0: StepConnection(); break;
            case 1: StepInput(); break;
            case 2: StepPreview(); break;
            case 3: StepExecute(); break;
        }
    }

    private Label H(string text) => new()
    {
        Text = text,
        ForeColor = Color.FromArgb(0, 55, 120),
        Font = new Font("Segoe UI", 18, FontStyle.Bold),
        Dock = DockStyle.Top,
        Height = 52
    };

    private Label Info(string text) => new()
    {
        Text = text,
        ForeColor = Color.FromArgb(40, 90, 145),
        Dock = DockStyle.Top,
        Height = 42
    };

    private void StepConnection()
    {
        if (string.IsNullOrWhiteSpace(_server.Text)) _server.Text = @".\SQLEXPRESS";
        if (string.IsNullOrWhiteSpace(_database.Text)) _database.Text = @"D:\DB_SPOIN\AMANSOFTS_20_10_2025.MDF";

        _content.Controls.Add(Card(panel =>
        {
            panel.Controls.Add(Info("اضغط مزامنة لاختبار الاتصال وتحميل أحدث الفواتير. الشريط الأخضر مؤشر خطوات فقط."));
            panel.Controls.Add(Field("مسار قاعدة البيانات", _database));
            panel.Controls.Add(Field("SQL Server", _server));
            panel.Controls.Add(H("1) الاتصال والمزامنة"));
        }));
    }

    private void StepInput()
    {
        SetupCombo(_invoiceBox);
        SetupCombo(_itemBox);
        SetupCombo(_priceBox);

        _invoiceBox.DropDownStyle = ComboBoxStyle.DropDown;
        _itemBox.DropDownStyle = ComboBoxStyle.DropDownList; // مهم: يمنع البحث العام خارج أصناف الفاتورة
        _priceBox.DropDownStyle = ComboBoxStyle.DropDown;

        _invoiceBox.SelectedIndexChanged -= InvoiceChanged;
        _invoiceBox.SelectedIndexChanged += InvoiceChanged;
        _itemBox.SelectedIndexChanged -= ItemChanged;
        _itemBox.SelectedIndexChanged += ItemChanged;

        _payEqualsTotal.Text = "";
        _payEqualsTotal.ForeColor = Color.FromArgb(255, 220, 120);
        _payEqualsTotal.Dock = DockStyle.Top;
        _payEqualsTotal.Height = 0;
        _payEqualsTotal.Checked = true;

        _content.Controls.Add(Card(panel =>
        {
            panel.Controls.Add(Info("اختر الفاتورة أولًا، ثم سيعرض البرنامج أصناف هذه الفاتورة فقط. هذا يمنع مشكلة ظهور أكثر من صنف."));
            panel.Controls.Add(_payEqualsTotal);
            panel.Controls.Add(Field("الكمية الجديدة", _qtyBox));
            panel.Controls.Add(Field("السعر الجديد / الافتراضي", _priceBox));
            panel.Controls.Add(Field("الصنف من الفاتورة فقط", _itemBox));
            panel.Controls.Add(Field("رقم الفاتورة S_ID", _invoiceBox));
            panel.Controls.Add(H("2) بيانات التعديل الذكي"));
        }));
    }

    private void StepPreview()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.BackgroundColor = Color.FromArgb(248, 252, 255);
        _grid.ForeColor = Color.FromArgb(0, 55, 120);
        _grid.GridColor = Color.FromArgb(180, 220, 245);
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(0, 80, 120);
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(0, 55, 120);
        _grid.EnableHeadersVisualStyles = false;
        _grid.DefaultCellStyle.BackColor = Color.FromArgb(250, 253, 255);
        _grid.DefaultCellStyle.ForeColor = Color.FromArgb(0, 55, 120);
        _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(180, 220, 255);
        _grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(0, 45, 100);
        _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(235, 246, 255);

        _content.Controls.Add(Card(panel =>
        {
            panel.Controls.Add(_grid);
            panel.Controls.Add(Info("اضغط معاينة. المعاينة الآن تعتمد على CLS_ID و SP_S_ID المختارين وليس LIKE عام."));
            panel.Controls.Add(H("3) معاينة قبل التعديل"));
        }));
    }

    private void StepExecute()
    {
        _confirm.Text = "أوافق على تنفيذ التعديل على قاعدة البيانات.";
        _confirm.ForeColor = Color.FromArgb(0, 75, 145);
        _confirm.Dock = DockStyle.Top;
        _confirm.Height = 42;

        _log.Multiline = true;
        _log.ReadOnly = true;
        _log.Dock = DockStyle.Fill;
        _log.BackColor = Color.FromArgb(242,248,255);
        _log.ForeColor = Color.FromArgb(0, 70, 145);
        _log.ScrollBars = ScrollBars.Vertical;

        _content.Controls.Add(Card(panel =>
        {
            panel.Controls.Add(_log);
            panel.Controls.Add(_confirm);
            panel.Controls.Add(Info("بعد التنفيذ ستظهر نافذة ختامية ورسالة نجاح."));
            panel.Controls.Add(H("4) تنفيذ وتوثيق"));
        }));
    }

    private static void SetupCombo(ComboBox c)
    {
        c.Dock = DockStyle.Top;
        c.Height = 36;
        c.BackColor = Color.White;
        c.ForeColor = Color.Black;
        c.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        c.AutoCompleteSource = AutoCompleteSource.ListItems;
    }

    private Control Field(string label, Control control)
    {
        var panel = new Panel { Dock = DockStyle.Top, Height = 70 };
        var l = new Label { Text = label, Dock = DockStyle.Top, Height = 28, ForeColor = Color.FromArgb(20, 75, 130) };
        control.Dock = DockStyle.Top;
        panel.Controls.Add(control);
        panel.Controls.Add(l);
        return panel;
    }

    private Panel Card(Action<Panel> build)
    {
        var outer = new GlowPanel { Dock = DockStyle.Fill, Padding = new Padding(22), AutoScroll = true, BackColor = Color.FromArgb(248, 252, 255) };
        build(outer);
        return outer;
    }

    private string ConnString()
    {
        var db = _database.Text.Trim().Replace("\"", "");
        return $"Server={_server.Text.Trim()};Database={db};Trusted_Connection=True;TrustServerCertificate=True;";
    }

    private async Task SyncCurrentStepAsync()
    {
        if (_step == 0) await TestConnectionAndLoadInvoicesAsync();
        else if (_step == 1)
        {
            await LoadInvoicesAsync();
            if (SelectedInvoice.HasValue) await LoadItemsForInvoiceAsync(SelectedInvoice.Value);
        }
    }

    private async Task TestConnectionAndLoadInvoicesAsync()
    {
        try
        {
            using var con = new SqlConnection(ConnString());
            await con.OpenAsync();
            await LoadInvoicesAsync(con);
            MessageBox.Show("تم الاتصال والمزامنة بنجاح.", "POS Data Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "فشل الاتصال", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task LoadInvoicesAsync(SqlConnection? existing = null)
    {
        bool close = existing is null;
        var con = existing ?? new SqlConnection(ConnString());
        if (close) await con.OpenAsync();

        try
        {
            var cmd = con.CreateCommand();
            cmd.CommandText = @"
SELECT TOP 20000
    S_ID,
    CAST(S_ID AS NVARCHAR(50)) + N' | ' +
    ISNULL(CONVERT(NVARCHAR(10), DOC_DATE, 120), N'بدون تاريخ') + N' | ' +
    CAST(CAST(ISNULL(DOC_TOT_FORIGNVALUE, DOC_FORIGNVALUE) AS DECIMAL(18,2)) AS NVARCHAR(50)) AS DisplayText
FROM SAL_MSTR_2
WHERE S_ID IS NOT NULL
ORDER BY DOC_DATE DESC, S_ID DESC;";
            var list = new List<InvoiceOption>();
            using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                list.Add(new InvoiceOption
                {
                    S_ID = Convert.ToInt64(rd["S_ID"]),
                    Display = Convert.ToString(rd["DisplayText"]) ?? ""
                });
            }

            _invoiceBox.BeginUpdate();
            _invoiceBox.Items.Clear();
            foreach (var x in list) _invoiceBox.Items.Add(x);
            _invoiceBox.EndUpdate();
        }
        finally
        {
            if (close) await con.DisposeAsync();
        }
    }

    private async void InvoiceChanged(object? sender, EventArgs e)
    {
        if (SelectedInvoice.HasValue) await LoadItemsForInvoiceAsync(SelectedInvoice.Value);
    }

    private async void ItemChanged(object? sender, EventArgs e)
    {
        var item = SelectedItemOption;
        if (item != null)
        {
            _qtyBox.Text = item.Qty.ToString("0.###", CultureInfo.InvariantCulture);
            await LoadPriceSuggestionsAsync(item);
        }
    }

    private async Task LoadItemsForInvoiceAsync(long invoice)
    {
        try
        {
            using var con = new SqlConnection(ConnString());
            await con.OpenAsync();
            var cmd = con.CreateCommand();
            cmd.CommandText = @"
SELECT
    d.CLS_ID,
    p.SP_S_ID,
    d.CLS_ARNAME,
    d.SD_PRICE,
    d.SD_QLT
FROM SAL_DETAIL d
LEFT JOIN SAL_POINT_INV p ON p.S_ID=d.S_ID
WHERE d.S_ID=@InvoiceNo
ORDER BY d.CLS_ARNAME;";
            cmd.Parameters.AddWithValue("@InvoiceNo", invoice);

            var list = new List<ItemOption>();
            using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                if (rd["SP_S_ID"] == DBNull.Value) continue;
                list.Add(new ItemOption
                {
                    CLS_ID = Convert.ToInt64(rd["CLS_ID"]),
                    SP_S_ID = Convert.ToInt64(rd["SP_S_ID"]),
                    Name = Convert.ToString(rd["CLS_ARNAME"]) ?? "",
                    OldPrice = Convert.ToDecimal(rd["SD_PRICE"]),
                    Qty = Convert.ToDecimal(rd["SD_QLT"])
                });
            }

            _itemBox.BeginUpdate();
            _itemBox.Items.Clear();
            _priceBox.Items.Clear();
            foreach (var x in list) _itemBox.Items.Add(x);
            _itemBox.EndUpdate();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "خطأ مزامنة الأصناف");
        }
    }

    private async Task LoadPriceSuggestionsAsync(ItemOption item)
    {
        try
        {
            using var con = new SqlConnection(ConnString());
            await con.OpenAsync();

            var suggestions = new List<decimal>();
            var defaultPrice = await GetDefaultClassPriceAsync(con, item.CLS_ID);
            if (defaultPrice.HasValue) suggestions.Add(defaultPrice.Value);

            suggestions.Add(item.OldPrice);

            var history = con.CreateCommand();
            history.CommandText = @"
SELECT DISTINCT TOP 10 CAST(SD_PRICE AS DECIMAL(18,2)) AS P
FROM SAL_DETAIL
WHERE CLS_ID=@CLS_ID AND SD_PRICE IS NOT NULL
ORDER BY P DESC;";
            history.Parameters.AddWithValue("@CLS_ID", item.CLS_ID);
            using var hr = await history.ExecuteReaderAsync();
            while (await hr.ReadAsync()) suggestions.Add(Convert.ToDecimal(hr["P"]));

            _priceBox.BeginUpdate();
            _priceBox.Items.Clear();
            foreach (var p in suggestions.Distinct().OrderByDescending(x => x))
                _priceBox.Items.Add(p.ToString("0.##"));
            if (_priceBox.Items.Count > 0) _priceBox.SelectedIndex = 0;
            _priceBox.EndUpdate();
        }
        catch
        {
            _priceBox.Items.Clear();
            _priceBox.Text = item.OldPrice.ToString("0.##");
        }
    }

    private async Task<decimal?> GetDefaultClassPriceAsync(SqlConnection con, long clsId)
    {
        var candidates = new[]
        {
            "CLS_PRICE","CLS_UN_1_PRICE","CLS_PRICE_1","CLS_SAL_PRICE","CLS_SALE_PRICE",
            "PRICE","SAL_PRICE","UNIT_PRICE","CLS_UN_PRICE"
        };

        var cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var c = con.CreateCommand();
        c.CommandText = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='CLASSES';";
        using (var rd = await c.ExecuteReaderAsync())
        {
            while (await rd.ReadAsync()) cols.Add(Convert.ToString(rd[0]) ?? "");
        }

        var found = candidates.FirstOrDefault(cols.Contains);
        if (found is null) return null;

        var cmd = con.CreateCommand();
        cmd.CommandText = $"SELECT TOP 1 CAST([{found}] AS DECIMAL(18,4)) AS P FROM CLASSES WHERE CLS_ID=@CLS_ID AND [{found}] IS NOT NULL;";
        cmd.Parameters.AddWithValue("@CLS_ID", clsId);
        var val = await cmd.ExecuteScalarAsync();
        return val == null || val == DBNull.Value ? null : Convert.ToDecimal(val);
    }

    private async Task PreviewAsync()
    {
        try
        {
            var inv = SelectedInvoice;
            var item = SelectedItemOption;
            var newPrice = SelectedPrice;

            if (!inv.HasValue) { MessageBox.Show("اختر رقم فاتورة صحيح.", "تنبيه"); return; }
            if (item == null) { MessageBox.Show("اختر الصنف من قائمة أصناف الفاتورة.", "تنبيه"); return; }
            if (!newPrice.HasValue) { MessageBox.Show("اختر أو اكتب السعر الجديد.", "تنبيه"); return; }
            if (SelectedQty <= 0) { MessageBox.Show("اكتب كمية صحيحة أكبر من صفر.", "تنبيه"); return; }

            using var con = new SqlConnection(ConnString());
            await con.OpenAsync();

            var cmd = con.CreateCommand();
            cmd.CommandText = @"
SELECT
    d.S_ID,
    p.SP_S_ID,
    d.CLS_ID,
    d.CLS_ARNAME,
    d.SD_PRICE AS OldPrice,
    d.SD_QLT AS Qty,
    CAST(@QtyNew AS DECIMAL(18,4)) AS NewQty,
    CAST(@NewPrice AS DECIMAL(18,4)) AS NewPrice,
    (CAST(@QtyNew AS DECIMAL(18,4)) * CAST(@NewPrice AS DECIMAL(18,4))) - (d.SD_QLT * d.SD_PRICE) AS Delta
FROM SAL_DETAIL d
LEFT JOIN SAL_POINT_INV p ON p.S_ID=d.S_ID
WHERE d.S_ID=@InvoiceNo
  AND d.CLS_ID=@CLS_ID
  AND p.SP_S_ID=@SP_S_ID;";
            cmd.Parameters.AddWithValue("@InvoiceNo", inv.Value);
            cmd.Parameters.AddWithValue("@CLS_ID", item.CLS_ID);
            cmd.Parameters.AddWithValue("@SP_S_ID", item.SP_S_ID);
            cmd.Parameters.AddWithValue("@NewPrice", newPrice.Value);
            cmd.Parameters.AddWithValue("@QtyNew", SelectedQty);

            var dt = new DataTable();
            using var rd = await cmd.ExecuteReaderAsync();
            dt.Load(rd);
            _previewTable = dt;
            _grid.DataSource = dt;
            ApplyPreviewGridHeaders();

            if (dt.Rows.Count == 0)
                MessageBox.Show("لم يتم العثور على الصنف المحدد داخل الفاتورة.", "لا توجد نتائج");
            else if (dt.Rows.Count > 1)
                MessageBox.Show("ظهر أكثر من سطر لنفس الصنف. لن يتم التنفيذ حتى يتم تضييق النتيجة.", "تنبيه");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "خطأ");
        }
    }

    private void ApplyPreviewGridHeaders()
    {
        SetGridHeader("S_ID", "رقم الفاتورة");
        SetGridHeader("SP_S_ID", "رقم نقطة البيع");
        SetGridHeader("CLS_ID", "رقم الصنف");
        SetGridHeader("CLS_ARNAME", "اسم الصنف");
        SetGridHeader("OldPrice", "السعر الحالي");
        SetGridHeader("NewPrice", "السعر الجديد");
        SetGridHeader("Qty", "الكمية الحالية");
        SetGridHeader("NewQty", "الكمية الجديدة");
        SetGridHeader("Delta", "فرق القيمة");

        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _grid.ColumnHeadersHeight = 38;
        _grid.RowTemplate.Height = 32;
    }

    private void SetGridHeader(string columnName, string headerText)
    {
        if (_grid.Columns.Contains(columnName))
            _grid.Columns[columnName].HeaderText = headerText;
    }

    private async Task ExecuteAsync()
    {
        try
        {
            if (!_confirm.Checked)
            {
                MessageBox.Show("يجب تفعيل الموافقة قبل التنفيذ.", "تنبيه");
                return;
            }
            if (_previewTable == null || _previewTable.Rows.Count != 1)
            {
                MessageBox.Show("يجب أن تحتوي المعاينة على سطر واحد فقط.", "تنبيه");
                return;
            }

            var row = _previewTable.Rows[0];
            long invoice = Convert.ToInt64(row["S_ID"]);
            long sp = Convert.ToInt64(row["SP_S_ID"]);
            long cls = Convert.ToInt64(row["CLS_ID"]);
            decimal oldPrice = Convert.ToDecimal(row["OldPrice"]);
            decimal qty = row.Table.Columns.Contains("OldQty") ? Convert.ToDecimal(row["OldQty"]) : Convert.ToDecimal(row["Qty"]);
            decimal newQty = row.Table.Columns.Contains("NewQty") ? Convert.ToDecimal(row["NewQty"]) : SelectedQty;
            decimal newPrice = Convert.ToDecimal(row["NewPrice"]);
            string itemName = Convert.ToString(row["CLS_ARNAME"]) ?? "";

            using var con = new SqlConnection(ConnString());
            await con.OpenAsync();

            using var tx = con.BeginTransaction();
            try
            {
                var cmd = con.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"
DECLARE @OldPrice DECIMAL(18,4);
DECLARE @Qty DECIMAL(18,4);
DECLARE @Delta DECIMAL(18,4);
DECLARE @InvoiceTotal DECIMAL(18,4);

SELECT @OldPrice=SD_PRICE,@Qty=SD_QLT
FROM SAL_DETAIL
WHERE S_ID=@InvoiceNo AND CLS_ID=@CLS_ID;

SET @Delta=(@QtyNew*@NewPrice)-(@Qty*@OldPrice);

UPDATE SAL_DETAIL
SET SD_QLT=@QtyNew,
    SD_PRICE=@NewPrice,
    SD_FORIGNVALUE=@QtyNew*@NewPrice,
    SD_TOT_FORIGNVALUE=@QtyNew*@NewPrice
WHERE S_ID=@InvoiceNo AND CLS_ID=@CLS_ID;

UPDATE SAL_POINT_INV_DET
SET SP_SD_QLT=@QtyNew,
    SP_SD_PRICE=@NewPrice,
    SP_SD_FORIGNVALUE=@QtyNew*@NewPrice,
    SP_SD_TOT_FORIGNVALUE=@QtyNew*@NewPrice
WHERE SP_S_ID=@SP_S_ID AND CLS_ID=@CLS_ID;

SELECT @InvoiceTotal = SUM(ISNULL(SP_SD_TOT_FORIGNVALUE,0))
FROM SAL_POINT_INV_DET
WHERE SP_S_ID=@SP_S_ID;

UPDATE SAL_POINT_INV
SET SP_S_FORIGNVALUE=@InvoiceTotal,
    SP_S_TOT_FORIGNVALUE=@InvoiceTotal,
    SP_S_PAY_FORIGNVALUE=CASE WHEN @PayEqualsTotal=1 THEN @InvoiceTotal ELSE SP_S_PAY_FORIGNVALUE+@Delta END
WHERE SP_S_ID=@SP_S_ID;

SELECT S_ID,DOC_FORIGNVALUE,DOC_TOT_FORIGNVALUE,DOC_PAY_FORIGNVALUE
FROM SAL_MSTR_2
WHERE S_ID=@InvoiceNo;";
                cmd.Parameters.AddWithValue("@InvoiceNo", invoice);
                cmd.Parameters.AddWithValue("@SP_S_ID", sp);
                cmd.Parameters.AddWithValue("@CLS_ID", cls);
                cmd.Parameters.AddWithValue("@NewPrice", newPrice);
                cmd.Parameters.AddWithValue("@QtyNew", newQty);
                cmd.Parameters.AddWithValue("@PayEqualsTotal", _payEqualsTotal.Checked ? 1 : 0);

                var dt = new DataTable();
                using var rd = await cmd.ExecuteReaderAsync();
                dt.Load(rd);
                tx.Commit();

                var delta = (newQty * newPrice) - (qty * oldPrice);
                var sb = new StringBuilder();
                sb.AppendLine("تم التعديل بنجاح.");
                sb.AppendLine($"الفاتورة: {invoice}");
                sb.AppendLine($"الصنف: {itemName}");
                sb.AppendLine($"SP_S_ID: {sp}");
                sb.AppendLine($"CLS_ID: {cls}");
                sb.AppendLine($"السعر القديم: {oldPrice}");
                sb.AppendLine($"السعر الجديد: {newPrice}");
                sb.AppendLine($"الكمية القديمة: {qty}");
                sb.AppendLine($"الكمية الجديدة: {newQty}");
                sb.AppendLine($"فرق القيمة: {delta}");
                sb.AppendLine();
                foreach (DataRow r in dt.Rows)
                {
                    sb.AppendLine($"DOC_FORIGNVALUE: {r["DOC_FORIGNVALUE"]}");
                    sb.AppendLine($"DOC_TOT_FORIGNVALUE: {r["DOC_TOT_FORIGNVALUE"]}");
                    sb.AppendLine($"DOC_PAY_FORIGNVALUE: {r["DOC_PAY_FORIGNVALUE"]}");
                }
                _log.Text = sb.ToString();

                MessageBox.Show(
                    $"تم التعديل بنجاح\n\nالفاتورة: {invoice}\nالصنف: {itemName}\nالسعر القديم: {oldPrice}\nالسعر الجديد: {newPrice}\nالكمية القديمة: {qty}\nالكمية الجديدة: {newQty}\nفرق القيمة: {delta}",
                    "POS Data Editor",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "خطأ أثناء التنفيذ");
        }
    }
}

public sealed class GlowPanel : Panel
{
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(Color.FromArgb(90, 0, 229, 255), 2);
        var rect = ClientRectangle;
        rect.Inflate(-2, -2);
        using var path = RoundedRect(rect, 18);
        e.Graphics.DrawPath(pen, path);
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        int d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}




























