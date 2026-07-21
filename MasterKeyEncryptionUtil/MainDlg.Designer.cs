using System.Windows.Forms;

namespace MasterKeyEncryptionUtil
{
    partial class MainDlg
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainDlg));
            comboBoxScopeNames = new ComboBox();
            label1 = new Label();
            label2 = new Label();
            passwordBox = new TextBox();
            storeButton = new Button();
            resultBox = new TextBox();
            label3 = new Label();
            buttonCopyToClipboard = new Button();
            toolTipStore = new ToolTip(components);
            buttonQuery = new Button();
            toolTipCopy = new ToolTip(components);
            button1 = new Button();
            tabControl1 = new TabControl();
            tabStore = new TabPage();
            tabQuery = new TabPage();
            queryPassPhrase = new TextBox();
            label7 = new Label();
            queryEncryptedPassPhrase = new TextBox();
            label6 = new Label();
            label5 = new Label();
            comboBoxStoreScopes = new ComboBox();
            tabControl1.SuspendLayout();
            tabStore.SuspendLayout();
            tabQuery.SuspendLayout();
            SuspendLayout();
            // 
            // comboBoxScopeNames
            // 
            comboBoxScopeNames.AllowDrop = true;
            comboBoxScopeNames.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxScopeNames.FormattingEnabled = true;
            comboBoxScopeNames.Items.AddRange(new object[] { "FIG_MASTER_KEY" });
            comboBoxScopeNames.Location = new Point(95, 14);
            comboBoxScopeNames.Name = "comboBoxScopeNames";
            comboBoxScopeNames.Size = new Size(202, 23);
            comboBoxScopeNames.TabIndex = 1;
            comboBoxScopeNames.SelectedIndexChanged += comboBoxScopeNames_SelectedIndexChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(6, 17);
            label1.Name = "label1";
            label1.RightToLeft = RightToLeft.No;
            label1.Size = new Size(65, 15);
            label1.TabIndex = 0;
            label1.Text = "Master Key";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(6, 53);
            label2.Name = "label2";
            label2.Size = new Size(68, 15);
            label2.TabIndex = 2;
            label2.Text = "Pass Phrase";
            // 
            // passwordBox
            // 
            passwordBox.Location = new Point(95, 50);
            passwordBox.Name = "passwordBox";
            passwordBox.PlaceholderText = "Enter passphrase";
            passwordBox.Size = new Size(630, 23);
            passwordBox.TabIndex = 3;
            passwordBox.TextChanged += passwordBox_TextChanged;
            // 
            // storeButton
            // 
            storeButton.Enabled = false;
            storeButton.Image = (Image)resources.GetObject("storeButton.Image");
            storeButton.ImageAlign = ContentAlignment.MiddleRight;
            storeButton.Location = new Point(637, 6);
            storeButton.Name = "storeButton";
            storeButton.Padding = new Padding(5, 0, 5, 0);
            storeButton.Size = new Size(88, 34);
            storeButton.TabIndex = 4;
            storeButton.Text = "Encrypt";
            storeButton.TextAlign = ContentAlignment.MiddleLeft;
            toolTipStore.SetToolTip(storeButton, "Add to Protected Data Store");
            storeButton.UseVisualStyleBackColor = true;
            storeButton.Click += storeButton_Click;
            // 
            // resultBox
            // 
            resultBox.Location = new Point(6, 122);
            resultBox.Multiline = true;
            resultBox.Name = "resultBox";
            resultBox.Size = new Size(719, 139);
            resultBox.TabIndex = 7;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(6, 102);
            label3.Name = "label3";
            label3.RightToLeft = RightToLeft.No;
            label3.Size = new Size(121, 15);
            label3.TabIndex = 6;
            label3.Text = "Encrypted Passphrase";
            // 
            // buttonCopyToClipboard
            // 
            buttonCopyToClipboard.BackgroundImageLayout = ImageLayout.None;
            buttonCopyToClipboard.FlatAppearance.BorderSize = 0;
            buttonCopyToClipboard.FlatStyle = FlatStyle.Flat;
            buttonCopyToClipboard.Image = (Image)resources.GetObject("buttonCopyToClipboard.Image");
            buttonCopyToClipboard.Location = new Point(699, 90);
            buttonCopyToClipboard.Margin = new Padding(0);
            buttonCopyToClipboard.Name = "buttonCopyToClipboard";
            buttonCopyToClipboard.Size = new Size(26, 29);
            buttonCopyToClipboard.TabIndex = 9;
            toolTipCopy.SetToolTip(buttonCopyToClipboard, "Copy encrypted pass phrase to clipboard");
            buttonCopyToClipboard.UseVisualStyleBackColor = true;
            buttonCopyToClipboard.Click += buttonCopyToClipboard_Click;
            // 
            // buttonQuery
            // 
            buttonQuery.Enabled = false;
            buttonQuery.ImageAlign = ContentAlignment.MiddleRight;
            buttonQuery.Location = new Point(637, 7);
            buttonQuery.Name = "buttonQuery";
            buttonQuery.Padding = new Padding(5, 0, 5, 0);
            buttonQuery.Size = new Size(88, 34);
            buttonQuery.TabIndex = 10;
            buttonQuery.Text = "Decrypt";
            buttonQuery.TextAlign = ContentAlignment.MiddleLeft;
            toolTipStore.SetToolTip(buttonQuery, "Add to Protected Data Store");
            buttonQuery.UseVisualStyleBackColor = true;
            buttonQuery.Click += buttonQuery_Click;
            // 
            // button1
            // 
            button1.BackgroundImageLayout = ImageLayout.None;
            button1.FlatAppearance.BorderSize = 0;
            button1.FlatStyle = FlatStyle.Flat;
            button1.Location = new Point(699, 206);
            button1.Margin = new Padding(0);
            button1.Name = "button1";
            button1.Size = new Size(26, 29);
            button1.TabIndex = 10;
            toolTipCopy.SetToolTip(button1, "Copy encrypted pass phrase to clipboard");
            button1.UseVisualStyleBackColor = true;
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabStore);
            tabControl1.Controls.Add(tabQuery);
            tabControl1.Location = new Point(9, 12);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(739, 295);
            tabControl1.TabIndex = 10;
            // 
            // tabStore
            // 
            tabStore.Controls.Add(label1);
            tabStore.Controls.Add(buttonCopyToClipboard);
            tabStore.Controls.Add(comboBoxScopeNames);
            tabStore.Controls.Add(resultBox);
            tabStore.Controls.Add(label3);
            tabStore.Controls.Add(storeButton);
            tabStore.Controls.Add(passwordBox);
            tabStore.Controls.Add(label2);
            tabStore.Location = new Point(4, 24);
            tabStore.Name = "tabStore";
            tabStore.Padding = new Padding(3);
            tabStore.Size = new Size(731, 267);
            tabStore.TabIndex = 0;
            tabStore.Text = "Store";
            tabStore.UseVisualStyleBackColor = true;
            // 
            // tabQuery
            // 
            tabQuery.Controls.Add(button1);
            tabQuery.Controls.Add(queryPassPhrase);
            tabQuery.Controls.Add(label7);
            tabQuery.Controls.Add(queryEncryptedPassPhrase);
            tabQuery.Controls.Add(buttonQuery);
            tabQuery.Controls.Add(label6);
            tabQuery.Controls.Add(label5);
            tabQuery.Controls.Add(comboBoxStoreScopes);
            tabQuery.Location = new Point(4, 24);
            tabQuery.Name = "tabQuery";
            tabQuery.Padding = new Padding(3);
            tabQuery.Size = new Size(731, 267);
            tabQuery.TabIndex = 1;
            tabQuery.Text = "Query";
            tabQuery.UseVisualStyleBackColor = true;
            // 
            // queryPassPhrase
            // 
            queryPassPhrase.Location = new Point(7, 238);
            queryPassPhrase.Name = "queryPassPhrase";
            queryPassPhrase.PlaceholderText = "Enter passphrase";
            queryPassPhrase.Size = new Size(719, 23);
            queryPassPhrase.TabIndex = 13;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(6, 217);
            label7.Name = "label7";
            label7.Size = new Size(68, 15);
            label7.TabIndex = 12;
            label7.Text = "Pass Phrase";
            // 
            // queryEncryptedPassPhrase
            // 
            queryEncryptedPassPhrase.Location = new Point(7, 69);
            queryEncryptedPassPhrase.Multiline = true;
            queryEncryptedPassPhrase.Name = "queryEncryptedPassPhrase";
            queryEncryptedPassPhrase.Size = new Size(718, 126);
            queryEncryptedPassPhrase.TabIndex = 11;
            queryEncryptedPassPhrase.TextChanged += queryEncryptedPassPhrase_TextChanged;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(6, 51);
            label6.Name = "label6";
            label6.RightToLeft = RightToLeft.No;
            label6.Size = new Size(121, 15);
            label6.TabIndex = 10;
            label6.Text = "Encrypted Passphrase";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(6, 17);
            label5.Name = "label5";
            label5.RightToLeft = RightToLeft.No;
            label5.Size = new Size(65, 15);
            label5.TabIndex = 2;
            label5.Text = "Master Key";
            // 
            // comboBoxStoreScopes
            // 
            comboBoxStoreScopes.AllowDrop = true;
            comboBoxStoreScopes.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxStoreScopes.FormattingEnabled = true;
            comboBoxStoreScopes.Items.AddRange(new object[] { "FIG_MASTER_KEY" });
            comboBoxStoreScopes.Location = new Point(95, 14);
            comboBoxStoreScopes.Name = "comboBoxStoreScopes";
            comboBoxStoreScopes.Size = new Size(202, 23);
            comboBoxStoreScopes.TabIndex = 3;
            comboBoxStoreScopes.SelectedIndexChanged += comboBoxStoreScopes_SelectedIndexChanged;
            // 
            // MainDlg
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(760, 315);
            Controls.Add(tabControl1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "MainDlg";
            Text = "Data Protection Util";
            Load += MainDlg_Load;
            tabControl1.ResumeLayout(false);
            tabStore.ResumeLayout(false);
            tabStore.PerformLayout();
            tabQuery.ResumeLayout(false);
            tabQuery.PerformLayout();
            ResumeLayout(false);

        }

        #endregion

        private Label label1;
        private Label label2;
        private TextBox passwordBox;
        private Button storeButton;
        private TextBox resultBox;
        private Label label3;
        private ComboBox comboBoxScopeNames;
        private Button buttonCopyToClipboard;
        private ToolTip toolTipStore;
        private ToolTip toolTipCopy;
        private TabControl tabControl1;
        private TabPage tabStore;
        private TabPage tabQuery;
        private Label label5;
        private ComboBox comboBoxStoreScopes;
        private Button buttonQuery;
        private TextBox queryEncryptedPassPhrase;
        private Label label6;
        private TextBox queryPassPhrase;
        private Label label7;
        private Button button1;
    }
}