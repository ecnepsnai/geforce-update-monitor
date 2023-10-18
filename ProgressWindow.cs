namespace GeforceUpdateMonitor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal class ProgressWindow : Form
    {
        public ProgressWindow()
        {
            Application.EnableVisualStyles();
            this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            this.ClientSize = new Size(500, 75);
            Label label = new Label
            {
                Text = "Please wait...",
                Location = new Point(5, 5),
                AutoSize = true
            };
            ProgressBar progressBar = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                Location = new Point(5, 30),
                Size = new Size(490, 30)
            };
            this.Controls.Add(label);
            this.Controls.Add(progressBar);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "ProgressWindow";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Installing GeForce Driver...";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
