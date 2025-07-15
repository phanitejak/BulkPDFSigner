namespace BulkPdfSigner;

partial class Form1
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
        this.toolStrip1 = new System.Windows.Forms.ToolStrip();
        this.aboutToolStrip = new System.Windows.Forms.ToolStripButton();
        this.groupBox1 = new System.Windows.Forms.GroupBox();
        this.label3 = new System.Windows.Forms.Label();
        this.sigLoc_listbox = new System.Windows.Forms.ComboBox();
        this.beginsigning = new System.Windows.Forms.Button();
        this.Browse_TF = new System.Windows.Forms.Button();
        this.Browse_SF = new System.Windows.Forms.Button();
        this.textBox2 = new System.Windows.Forms.TextBox();
        this.textBox1 = new System.Windows.Forms.TextBox();
        this.label2 = new System.Windows.Forms.Label();
        this.label1 = new System.Windows.Forms.Label();
        this.groupBox2 = new System.Windows.Forms.GroupBox();
        this.status_msgbox = new System.Windows.Forms.TextBox();
        this.pgbar = new System.Windows.Forms.ProgressBar();
        this.msglabel = new System.Windows.Forms.Label();
        this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
        this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
        this.toolStrip1.SuspendLayout();
        this.groupBox1.SuspendLayout();
        this.groupBox2.SuspendLayout();
        this.SuspendLayout();
        // 
        // toolStrip1
        // 
        this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStrip});
        this.toolStrip1.Location = new System.Drawing.Point(0, 0);
        this.toolStrip1.Name = "toolStrip1";
        this.toolStrip1.Size = new System.Drawing.Size(638, 25);
        this.toolStrip1.TabIndex = 0;
        this.toolStrip1.Text = "toolStrip1";
        // 
        // aboutToolStrip
        // 
        this.aboutToolStrip.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
        this.aboutToolStrip.Image = ((System.Drawing.Image)(resources.GetObject("aboutToolStrip.Image")));
        this.aboutToolStrip.ImageTransparentColor = System.Drawing.Color.Magenta;
        this.aboutToolStrip.Name = "aboutToolStrip";
        this.aboutToolStrip.Size = new System.Drawing.Size(44, 22);
        this.aboutToolStrip.Text = "About";
        this.aboutToolStrip.Click += new System.EventHandler(this.aboutToolStrip_Click);
        // 
        // groupBox1
        // 
        this.groupBox1.Controls.Add(this.label3);
        this.groupBox1.Controls.Add(this.sigLoc_listbox);
        this.groupBox1.Controls.Add(this.beginsigning);
        this.groupBox1.Controls.Add(this.Browse_TF);
        this.groupBox1.Controls.Add(this.Browse_SF);
        this.groupBox1.Controls.Add(this.textBox2);
        this.groupBox1.Controls.Add(this.textBox1);
        this.groupBox1.Controls.Add(this.label2);
        this.groupBox1.Controls.Add(this.label1);
        this.groupBox1.Location = new System.Drawing.Point(13, 28);
        this.groupBox1.Name = "groupBox1";
        this.groupBox1.Size = new System.Drawing.Size(614, 67);
        this.groupBox1.TabIndex = 1;
        this.groupBox1.TabStop = false;
        this.groupBox1.Text = "Sign PDF";
        // 
        // label3
        // 
        this.label3.AutoSize = true;
        this.label3.Location = new System.Drawing.Point(400, 20);
        this.label3.Name = "label3";
        this.label3.Size = new System.Drawing.Size(96, 13);
        this.label3.TabIndex = 8;
        this.label3.Text = "Signature Location";
        // 
        // sigLoc_listbox
        // 
        this.sigLoc_listbox.AccessibleRole = System.Windows.Forms.AccessibleRole.OutlineButton;
        this.sigLoc_listbox.FormattingEnabled = true;
        this.sigLoc_listbox.Items.AddRange(new object[] {
            "Default",
            "Last Page"});
        this.sigLoc_listbox.Location = new System.Drawing.Point(400, 39);
        this.sigLoc_listbox.Name = "sigLoc_listbox";
        this.sigLoc_listbox.Size = new System.Drawing.Size(121, 21);
        this.sigLoc_listbox.TabIndex = 7;
        this.sigLoc_listbox.SelectedIndexChanged += new System.EventHandler(this.sigLoc_listbox_SelectedIndexChanged);
        // 
        // beginsigning
        // 
        this.beginsigning.Location = new System.Drawing.Point(541, 11);
        this.beginsigning.Name = "beginsigning";
        this.beginsigning.Size = new System.Drawing.Size(67, 56);
        this.beginsigning.TabIndex = 6;
        this.beginsigning.Text = "Begin Signing";
        this.beginsigning.UseVisualStyleBackColor = true;
        this.beginsigning.Click += new System.EventHandler(this.beginsigning_Click_1);
        // 
        // Browse_TF
        // 
        this.Browse_TF.Location = new System.Drawing.Point(357, 40);
        this.Browse_TF.Name = "Browse_TF";
        this.Browse_TF.Size = new System.Drawing.Size(25, 19);
        this.Browse_TF.TabIndex = 5;
        this.Browse_TF.Text = "...";
        this.Browse_TF.UseVisualStyleBackColor = true;
        this.Browse_TF.Click += new System.EventHandler(this.Browse_TF_Click);
        // 
        // Browse_SF
        // 
        this.Browse_SF.Location = new System.Drawing.Point(357, 19);
        this.Browse_SF.Name = "Browse_SF";
        this.Browse_SF.Size = new System.Drawing.Size(25, 19);
        this.Browse_SF.TabIndex = 4;
        this.Browse_SF.Text = "...";
        this.Browse_SF.UseVisualStyleBackColor = true;
        this.Browse_SF.Click += new System.EventHandler(this.Browse_SF_Click);
        // 
        // textBox2
        // 
        this.textBox2.Location = new System.Drawing.Point(100, 40);
        this.textBox2.Name = "textBox2";
        this.textBox2.Size = new System.Drawing.Size(251, 20);
        this.textBox2.TabIndex = 3;
        // 
        // textBox1
        // 
        this.textBox1.Location = new System.Drawing.Point(100, 19);
        this.textBox1.Name = "textBox1";
        this.textBox1.Size = new System.Drawing.Size(251, 20);
        this.textBox1.TabIndex = 2;
        // 
        // label2
        // 
        this.label2.AutoSize = true;
        this.label2.Location = new System.Drawing.Point(2, 43);
        this.label2.Name = "label2";
        this.label2.Size = new System.Drawing.Size(92, 13);
        this.label2.TabIndex = 1;
        this.label2.Text = "Destination Folder";
        // 
        // label1
        // 
        this.label1.AutoSize = true;
        this.label1.Location = new System.Drawing.Point(21, 19);
        this.label1.Name = "label1";
        this.label1.Size = new System.Drawing.Size(73, 13);
        this.label1.TabIndex = 0;
        this.label1.Text = "Source Folder";
        // 
        // groupBox2
        // 
        this.groupBox2.Controls.Add(this.status_msgbox);
        this.groupBox2.Location = new System.Drawing.Point(13, 101);
        this.groupBox2.Name = "groupBox2";
        this.groupBox2.Size = new System.Drawing.Size(613, 275);
        this.groupBox2.TabIndex = 2;
        this.groupBox2.TabStop = false;
        this.groupBox2.Text = "Status Message";
        // 
        // status_msgbox
        // 
        this.status_msgbox.Location = new System.Drawing.Point(7, 20);
        this.status_msgbox.Multiline = true;
        this.status_msgbox.Name = "status_msgbox";
        this.status_msgbox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
        this.status_msgbox.Size = new System.Drawing.Size(600, 249);
        this.status_msgbox.TabIndex = 0;
        // 
        // pgbar
        // 
        this.pgbar.Location = new System.Drawing.Point(13, 405);
        this.pgbar.Name = "pgbar";
        this.pgbar.Size = new System.Drawing.Size(613, 23);
        this.pgbar.TabIndex = 3;
        // 
        // msglabel
        // 
        this.msglabel.AutoSize = true;
        this.msglabel.Location = new System.Drawing.Point(293, 379);
        this.msglabel.Name = "msglabel";
        this.msglabel.Size = new System.Drawing.Size(13, 13);
        this.msglabel.TabIndex = 4;
        this.msglabel.Text = "d";
        // 
        // openFileDialog1
        // 
        this.openFileDialog1.FileName = "openFileDialog1";
        // 
        // Form1
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(638, 440);
        this.Controls.Add(this.msglabel);
        this.Controls.Add(this.pgbar);
        this.Controls.Add(this.groupBox2);
        this.Controls.Add(this.groupBox1);
        this.Controls.Add(this.toolStrip1);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
        this.MaximizeBox = false;
        this.Name = "Form1";
        this.Load += new System.EventHandler(this.Form1_Load);
        this.toolStrip1.ResumeLayout(false);
        this.toolStrip1.PerformLayout();
        this.groupBox1.ResumeLayout(false);
        this.groupBox1.PerformLayout();
        this.groupBox2.ResumeLayout(false);
        this.groupBox2.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.ToolStrip toolStrip1;
    private System.Windows.Forms.ToolStripButton aboutToolStrip;
    private System.Windows.Forms.GroupBox groupBox1;
    private System.Windows.Forms.Button Browse_SF;
    private System.Windows.Forms.TextBox textBox2;
    private System.Windows.Forms.TextBox textBox1;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Button Browse_TF;
    private System.Windows.Forms.GroupBox groupBox2;
    private System.Windows.Forms.TextBox status_msgbox;
    private System.Windows.Forms.ProgressBar pgbar;
    private System.Windows.Forms.Label msglabel;
    private System.Windows.Forms.OpenFileDialog openFileDialog1;
    private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
    private System.Windows.Forms.Button beginsigning;
    private System.Windows.Forms.Label label3;
    private System.Windows.Forms.ComboBox sigLoc_listbox;
}