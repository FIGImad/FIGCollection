using System.Security.Cryptography;

namespace MasterKeyEncryptionUtil
{
    public partial class MainDlg : Form
    {
        public MainDlg()
        {
            InitializeComponent();
        }

        private void storeButton_Click(object sender, EventArgs e)
        {
            if (passwordBox.Text.Length > 0)
            {
                // get value from comboBoxScopeNames
                // get value from passPhrase ??
                string selItem = ((string?)(comboBoxScopeNames?.SelectedItem)) ?? "";
                if (selItem == null)
                {
                    MessageBox.Show("Please select a Master key first");
                    return;
                }

                string? passPhraseEncrypted = ProtectedDataUtil.Protect(passwordBox.Text, selItem);
                if (passPhraseEncrypted == null) {
                    MessageBox.Show("Error encrypting the passphrase");
                    return;
                }
                resultBox.Text = passPhraseEncrypted;
            }
            else
            {
                MessageBox.Show("Please enter a passphrase");
            }
        }

        private void passwordBox_TextChanged(object sender, EventArgs e)
        {
            handleStoreButton();
        }
        private void comboBoxScopeNames_SelectedIndexChanged(object sender, EventArgs e)
        {
            handleStoreButton();
        }

        private void buttonCopyToClipboard_Click(object sender, EventArgs e)
        {
            if (resultBox.Text.Length > 0)
            {
                Clipboard.SetText(resultBox.Text);
            }
        }

        private void MainDlg_Load(object sender, EventArgs e)
        {

        }

        private void queryEncryptedPassPhrase_TextChanged(object sender, EventArgs e)
        {
            handleQueryButton();
        }

        private void comboBoxStoreScopes_SelectedIndexChanged(object sender, EventArgs e)
        {
            handleQueryButton();
        }

        private void buttonQuery_Click(object sender, EventArgs e)
        {
            if (queryEncryptedPassPhrase.Text.Length > 0)
            {
                string selItem = ((string?)(comboBoxStoreScopes?.SelectedItem)) ?? "";
                if (selItem == null)
                {
                    MessageBox.Show("Please select a store location");
                    return;
                }
                DataProtectionScope scope = selItem == "LocalMachine" ? DataProtectionScope.LocalMachine : DataProtectionScope.CurrentUser;
                string? passPhrase = ProtectedDataUtil.Unprotect(queryEncryptedPassPhrase.Text);
                if (passPhrase == null)
                {
                    MessageBox.Show("Error decrypting the passphrase. Please ensure the encrypted passphrase and store location are correct.");
                    return;
                }
                queryPassPhrase.Text = passPhrase ?? "Error decrypting the passphrase";
            }
        }

        private void handleStoreButton()
        {
            string selItem = ((string?)(comboBoxScopeNames?.SelectedItem)) ?? "";
            storeButton.Enabled = !string.IsNullOrWhiteSpace(passwordBox.Text) && selItem.Length > 0;
        }
        private void handleQueryButton()
        {
            string selItem = ((string?)(comboBoxStoreScopes?.SelectedItem)) ?? "";
            buttonQuery.Enabled = !string.IsNullOrWhiteSpace(queryEncryptedPassPhrase.Text) && selItem.Length > 0;
        }
    }
}