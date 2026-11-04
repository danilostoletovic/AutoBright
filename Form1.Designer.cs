#nullable disable
namespace AutoBright;

partial class Form1
{
    private System.ComponentModel.IContainer components;
    private Label titleLabel;
    private Button toggleButton;
    private Label statusLabel;
    private Label nextLabel;
    private Label attentionLabel;
    private LinkLabel locationLink;
    private ComboBox cameras;
    private Label cameraLabel;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code
    private void InitializeComponent()
    {

        components = new System.ComponentModel.Container();
        titleLabel = new Label();
        toggleButton = new Button();
        statusLabel = new Label();
        nextLabel = new Label();
        attentionLabel = new Label();
        locationLink = new LinkLabel();
        cameras = new ComboBox();
        cameraLabel = new Label();
        SuspendLayout();
        titleLabel.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
        titleLabel.Location = new Point(24, 20);
        titleLabel.Size = new Size(352, 44);
        titleLabel.Text = "AutoBright";
        toggleButton.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
        toggleButton.Location = new Point(24, 84);
        toggleButton.Size = new Size(352, 64);
        toggleButton.Text = "OFF";
        toggleButton.TabIndex = 0;
        toggleButton.UseVisualStyleBackColor = true;
        toggleButton.AccessibleName = "Automatic brightness adjustment ON or OFF";
        toggleButton.Click += ToggleButton_Click;
        statusLabel.Location = new Point(24, 162);
        statusLabel.Size = new Size(352, 26);
        statusLabel.Text = "Automatic brightness disabled";
        nextLabel.Location = new Point(24, 195);
        nextLabel.Size = new Size(352, 48);
        nextLabel.Text = "Camera-based brightness. Every two minutes.";
        attentionLabel.Location = new Point(24, 250);
        attentionLabel.Size = new Size(352, 80);
        locationLink.Location = new Point(24, 339);
        locationLink.Size = new Size(352, 26);
        locationLink.Text = "Sample now";
        locationLink.TabIndex = 1;
        locationLink.LinkClicked += SampleLink_LinkClicked;
        cameraLabel.Location = new Point(24, 378);
        cameraLabel.Size = new Size(352, 25);
        cameraLabel.Text = "Camera";
        cameras.Location = new Point(24, 407);
        cameras.Size = new Size(352, 28);
        cameras.DropDownStyle = ComboBoxStyle.DropDownList;
        cameras.AccessibleName = "Camera";
        cameras.TabIndex = 2;
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(400, 470);
        Controls.Add(cameraLabel);
        Controls.Add(cameras);
        Controls.Add(titleLabel);
        Controls.Add(toggleButton);
        Controls.Add(statusLabel);
        Controls.Add(nextLabel);
        Controls.Add(attentionLabel);
        Controls.Add(locationLink);
        Font = new Font("Segoe UI", 10F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Name = "Form1";
        Text = "AutoBright";

        ResumeLayout(false);
    }
    #endregion
}
