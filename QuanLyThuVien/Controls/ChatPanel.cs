using QuanLyThuVien.Chat.Contracts;
using QuanLyThuVien.Helpers;
using QuanLyThuVien.Services;

namespace QuanLyThuVien.Controls;

public sealed class ChatPanel : UserControl
{
    private readonly ChatApiClient _client;
    private readonly Action _close;
    private readonly Action<int> _openBook;
    private readonly FlowLayoutPanel _messages;
    private readonly ModernTextBox _input;
    private readonly ModernButton _send;
    private readonly ModernButton _stop;
    private readonly Label _status;
    private readonly List<ChatHistoryItem> _history = new();
    private readonly string _sessionId = Guid.NewGuid().ToString("N");
    private CancellationTokenSource? _requestCts;
    private string _lastMessage = string.Empty;

    public ChatPanel(ChatApiClient client, Action close, Action<int> openBook)
    {
        _client = client;
        _close = close;
        _openBook = openBook;
        Dock = DockStyle.None;
        BackColor = AppColors.CardBg;
        BorderStyle = BorderStyle.FixedSingle;
        AccessibleRole = AccessibleRole.Grouping;
        AccessibleName = "Trợ lý thư viện";

        var header = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = AppColors.Primary, Padding = new Padding(16, 10, 10, 8) };
        var title = new Label { Text = "Trợ lý thư viện", AutoSize = true, Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.White, Location = new Point(16, 10) };
        var subtitle = new Label { Text = "Tra cứu danh mục · chỉ đọc", AutoSize = true, Font = new Font("Segoe UI", 8F), ForeColor = AppColors.PrimaryLight, Location = new Point(16, 34) };
        var closeButton = new ModernButton { Text = "Đóng", Size = new Size(64, 34), BaseColor = AppColors.PrimaryDark, HoverColor = AppColors.SidebarBg, PressedColor = AppColors.SidebarBg, BorderRadius = 8, TextColor = Color.White, AccessibleName = "Đóng trợ lý", Anchor = AnchorStyles.Top | AnchorStyles.Right };
        closeButton.Click += (_, _) => _close();
        header.Resize += (_, _) => closeButton.Location = new Point(header.ClientSize.Width - closeButton.Width - 10, 14);
        closeButton.Location = new Point(header.ClientSize.Width - closeButton.Width - 10, 14);
        header.Controls.AddRange(new Control[] { title, subtitle, closeButton });

        _messages = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = AppColors.ContentBg, Padding = new Padding(8), TabIndex = 1, AccessibleRole = AccessibleRole.List, AccessibleName = "Lịch sử hội thoại" };
        _messages.SizeChanged += (_, _) => ResizeMessageChildren();

        var composer = new Panel { Dock = DockStyle.Bottom, Height = 112, BackColor = AppColors.CardBg, Padding = new Padding(10, 8, 10, 8) };
        _input = new ModernTextBox { Multiline = true, Placeholder = "Hỏi về sách trong thư viện...", Size = new Size(270, 62), Location = new Point(10, 8), Font = new Font("Segoe UI", 10F), AccessibleName = "Câu hỏi cho trợ lý", TabIndex = 0, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        _send = new ModernButton { Text = "Gửi", Size = new Size(74, 40), BaseColor = AppColors.Primary, HoverColor = AppColors.PrimaryDark, PressedColor = AppColors.SidebarBg, BorderRadius = 10, AccessibleName = "Gửi câu hỏi", TabIndex = 2, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _stop = new ModernButton { Text = "Dừng", Size = new Size(74, 32), BaseColor = AppColors.TextSecondary, HoverColor = AppColors.PrimaryDark, BorderRadius = 10, Visible = false, AccessibleName = "Dừng trả lời", TabIndex = 3, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _status = new Label { Text = "", AutoSize = true, ForeColor = AppColors.TextSecondary, Font = new Font("Segoe UI", 8F), Location = new Point(10, 78), AccessibleRole = AccessibleRole.StaticText };
        composer.Controls.AddRange(new Control[] { _input, _send, _stop, _status });
        composer.Resize += (_, _) =>
        {
            _send.Location = new Point(composer.ClientSize.Width - _send.Width - 10, 8);
            _stop.Location = new Point(composer.ClientSize.Width - _stop.Width - 10, 54);
            _input.Width = Math.Max(160, composer.ClientSize.Width - _send.Width - 30);
        };
        _send.Location = new Point(composer.ClientSize.Width - _send.Width - 10, 8);
        _stop.Location = new Point(composer.ClientSize.Width - _stop.Width - 10, 54);
        _input.Width = Math.Max(160, composer.ClientSize.Width - _send.Width - 30);

        Controls.Add(_messages);
        Controls.Add(composer);
        Controls.Add(header);

        _send.Click += async (_, _) => await SendAsync();
        _stop.Click += (_, _) => _requestCts?.Cancel();
        if (_input.Controls.Count > 0 && _input.Controls[0] is TextBox textBox)
            textBox.KeyDown += Input_KeyDown;

        AddAssistantMessage("Xin chào! Tôi có thể tìm sách, kiểm tra số lượng còn trong kho và gợi ý sách theo thể loại.");
        AddSuggestions();
    }

    public void FocusInput() => _input.Focus();

    private void Input_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && !e.Shift)
        {
            e.SuppressKeyPress = true;
            _ = SendAsync();
        }
        else if (e.KeyCode == Keys.Escape)
        {
            e.SuppressKeyPress = true;
            _close();
        }
    }

    private async Task SendAsync()
    {
        if (_requestCts != null) return;
        string message = _input.GetRealText().Trim();
        if (string.IsNullOrWhiteSpace(message)) return;
        if (message.Length > 1000)
        {
            _status.Text = "Câu hỏi tối đa 1.000 ký tự.";
            return;
        }

        _lastMessage = message;
        _status.Text = string.Empty;
        _input.Text = string.Empty;
        AddUserMessage(message);
        var assistant = AddAssistantMessage(string.Empty);
        bool completed = false;
        _requestCts = new CancellationTokenSource();
        SetBusy(true);
        try
        {
            var request = new ChatRequest(_sessionId, message, _history.TakeLast(8).ToArray());
            _history.Add(new ChatHistoryItem("user", message));
            await _client.StreamAsync(request,
                text => { assistant.AppendText(text); ScrollToBottom(); return Task.CompletedTask; },
                books => { AddBookCards(books); return Task.CompletedTask; },
                () => { completed = true; _status.Text = string.Empty; _history.Add(new ChatHistoryItem("assistant", assistant.MessageText)); TrimHistory(); return Task.CompletedTask; },
                error => { _status.Text = error.Text ?? "Trợ lý tạm thời không phản hồi."; AddError(error); return Task.CompletedTask; },
                _requestCts.Token);
        }
        catch (OperationCanceledException) when (_requestCts.IsCancellationRequested)
        {
            _status.Text = "Đã dừng trả lời.";
            RemoveIfEmpty(assistant);
        }
        catch (ChatApiException ex)
        {
            RemoveIfEmpty(assistant);
            AddError(new ChatStreamEvent("error", Text: ex.Message, ErrorCode: "client_error", Retryable: ex.Retryable));
            _status.Text = ex.Message;
        }
        catch (Exception ex)
        {
            RemoveIfEmpty(assistant);
            System.Diagnostics.Debug.WriteLine($"Chat UI error: {ex}");
            _status.Text = "Không thể hiển thị câu trả lời.";
        }
        finally
        {
            if (!completed && _history.Count > 0 && _history[^1] is { Role: "user" } last && last.Content == message)
                _history.RemoveAt(_history.Count - 1);
            _requestCts.Dispose();
            _requestCts = null;
            SetBusy(false);
            FocusInput();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _requestCts?.Cancel();
            _requestCts?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void SetBusy(bool busy)
    {
        _send.Enabled = !busy;
        _input.Enabled = !busy;
        _stop.Visible = busy;
        if (busy) _status.Text = "Đang tìm trong kho sách và trả lời...";
    }

    private void RemoveIfEmpty(ChatMessageControl message)
    {
        if (!string.IsNullOrWhiteSpace(message.MessageText)) return;
        _messages.Controls.Remove(message);
        message.Dispose();
    }

    private void TrimHistory()
    {
        while (_history.Count > ChatRequestValidator.MaxHistoryItems)
            _history.RemoveAt(0);
    }

    private ChatMessageControl AddUserMessage(string text) => AddMessage(new ChatMessageControl(true, text));
    private ChatMessageControl AddAssistantMessage(string text) => AddMessage(new ChatMessageControl(false, text));
    private ChatMessageControl AddMessage(ChatMessageControl message)
    {
        _messages.Controls.Add(message);
        ResizeMessageChildren();
        ScrollToBottom();
        return message;
    }

    private void AddBookCards(IReadOnlyList<BookSuggestion> books)
    {
        foreach (var book in books)
        {
            var card = new BookSuggestionCard(book);
            card.BookClicked += (_, maSach) => _openBook(maSach);
            _messages.Controls.Add(card);
        }
        ResizeMessageChildren();
        ScrollToBottom();
    }

    private void AddError(ChatStreamEvent error)
    {
        var errorBox = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = AppColors.CardBg, Padding = new Padding(8), Margin = new Padding(4, 4, 4, 8) };
        errorBox.Controls.Add(new Label { Text = error.Text ?? (error.ErrorCode == "client_error" ? "Không kết nối được trợ lý." : "Trợ lý tạm thời không phản hồi."), AutoSize = true, MaximumSize = new Size(320, 0), ForeColor = AppColors.Danger, Font = new Font("Segoe UI", 9F) });
        if (error.Retryable && !string.IsNullOrWhiteSpace(_lastMessage))
        {
            var retry = new ModernButton { Text = "Thử lại", Size = new Size(78, 32), BaseColor = AppColors.Primary, HoverColor = AppColors.PrimaryDark, BorderRadius = 8, AccessibleName = "Thử lại câu hỏi" };
            retry.Click += async (_, _) => await SendLastAsync();
            errorBox.Controls.Add(retry);
        }
        _messages.Controls.Add(errorBox);
        ResizeMessageChildren();
        ScrollToBottom();
    }

    private async Task SendLastAsync()
    {
        if (string.IsNullOrWhiteSpace(_lastMessage) || _requestCts != null) return;
        _input.Text = _lastMessage;
        await SendAsync();
    }

    private void AddSuggestions()
    {
        var suggestions = new[] { "Sách văn học còn trong kho", "Gợi ý truyện tranh", "Tìm sách của Nguyễn Nhật Ánh" };
        foreach (var suggestion in suggestions)
        {
            var button = new ModernButton { Text = suggestion, AutoSize = true, Height = 32, BaseColor = AppColors.CardBg, HoverColor = AppColors.PrimaryLight, PressedColor = AppColors.SelectedSurface, TextColor = AppColors.PrimaryDark, BorderRadius = 10, AccessibleName = $"Hỏi: {suggestion}" };
            button.Click += (_, _) => { _input.Text = suggestion; FocusInput(); };
            _messages.Controls.Add(button);
        }
    }

    private void ResizeMessageChildren()
    {
        int width = Math.Max(220, _messages.ClientSize.Width - _messages.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth - 8);
        foreach (Control child in _messages.Controls)
        {
            if (child is BookSuggestionCard) child.Width = Math.Min(344, width);
            else if (child is ChatMessageControl) child.Width = Math.Min(350, width);
            else if (child is FlowLayoutPanel) child.Width = width;
        }
    }

    private void ScrollToBottom()
    {
        if (_messages.Controls.Count > 0)
            _messages.ScrollControlIntoView(_messages.Controls[^1]);
    }
}
