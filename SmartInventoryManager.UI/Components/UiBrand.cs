namespace SmartInventoryManager.UI.Components;

internal static class UiBrand
{
    public const string ApplicationName = "VioWare";
    public const string ProjectName = "Smart Inventory Manager";
    public static void ApplyIcon(Form form)
    {
        using var stream = typeof(UiBrand).Assembly.GetManifestResourceStream("SmartInventoryManager.UI.ApplicationIcon")
            ?? throw new InvalidOperationException("Missing application icon resource");
        using var source = new Icon(stream);
        var icon = (Icon)source.Clone();
        form.Icon = icon;
        form.Disposed += (_, _) => icon.Dispose();
    }

    public static PictureBox CreateLogo()
    {
        using var stream = typeof(UiBrand).Assembly.GetManifestResourceStream("SmartInventoryManager.UI.ProjectLogo")
            ?? throw new InvalidOperationException("Missing project logo resource");
        using var original = Image.FromStream(stream);
        var logo = new PictureBox
        {
            Image = new Bitmap(original), SizeMode = PictureBoxSizeMode.Zoom,
            AccessibleName = "Logo VioWare", TabStop = false,
            // [COLOR CONFIG]: nền trong suốt để logo hòa vào nền Form, không có ô màu phía sau.
            BackColor = Color.Transparent
        };
        logo.Disposed += (_, _) => logo.Image?.Dispose();
        return logo;
    }
}
