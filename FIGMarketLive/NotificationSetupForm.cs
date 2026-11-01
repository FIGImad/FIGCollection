namespace FIGMarketLive;

internal sealed class NotificationSetupForm : Form
{
    private readonly ComboBox authentication=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=300};
    private readonly TextBox username=new(){Width=300};
    private readonly TextBox password=new(){Width=300,UseSystemPasswordChar=true};
    private readonly Label status=new(){AutoSize=true,MaximumSize=new(580,0)};
    private readonly Button execute=new(){Text="Configure notifications",AutoSize=true};
    private readonly Button close=new(){Text="Cancel",AutoSize=true};
    private bool busy;
    public NotificationSetupForm(NotificationAdministratorSetup setup,NotificationSetupTarget target,Func<string,Task>? beforeChange)
    {
        Text="Configure SQL notifications";Width=660;Height=450;MinimumSize=new(620,440);StartPosition=FormStartPosition.CenterParent;
        var layout=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new(14)};
        layout.Controls.Add(new Label{AutoSize=true,MaximumSize=new(590,0),Text=$"Database: {target.Database}\r\nApplication login: {target.Login}\r\nDatabase user receiving permissions: {target.DatabaseUser}\r\n\r\nIf Broker is disabled, setup uses SINGLE_USER WITH ROLLBACK IMMEDIATE. This disconnects other database sessions and rolls back their uncommitted transactions. MULTI_USER is restored afterward. FIGMarketLive workers will stop before setup.\r\n\r\nUse the configured connection if authorized, or enter SQL administrator credentials for this operation only. Administrator credentials are never saved."});
        authentication.Items.AddRange(["Use configured connection","SQL administrator username/password"]);authentication.SelectedIndex=0;
        layout.Controls.Add(authentication);layout.Controls.Add(new Label{Text="Administrator username",AutoSize=true});layout.Controls.Add(username);
        layout.Controls.Add(new Label{Text="Administrator password",AutoSize=true});layout.Controls.Add(password);
        var actions=new FlowLayoutPanel{AutoSize=true};actions.Controls.AddRange([execute,close]);layout.Controls.Add(actions);layout.Controls.Add(status);Controls.Add(layout);
        void RefreshAuthentication(){username.Enabled=password.Enabled=authentication.SelectedIndex==1;}
        authentication.SelectedIndexChanged+=(_,_)=>RefreshAuthentication();RefreshAuthentication();
        close.Click+=(_,_)=>Close();
        FormClosing+=(_,e)=>{if(busy)e.Cancel=true;else password.Clear();};
        execute.Click+=async(_,_)=>
        {
            if(busy)return;
            if(authentication.SelectedIndex==1&&string.IsNullOrWhiteSpace(username.Text)){status.Text="Enter the administrator username.";return;}
            if(MessageBox.Show(this,$"Configure notifications for '{target.Database}'?\n\nIf Broker is disabled, other sessions will be disconnected and uncommitted work rolled back. The database will return to MULTI_USER. Continue?","Confirm database notification setup",MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return;
            busy=true;execute.Enabled=close.Enabled=authentication.Enabled=username.Enabled=password.Enabled=false;UseWaitCursor=true;
            try
            {
                if(beforeChange is not null)await beforeChange("configuring SQL notifications");
                await setup.ConfigureAsync(target,authentication.SelectedIndex==1?username.Text.Trim():null,authentication.SelectedIndex==1?password.Text:null,default);
                status.Text="Broker and application-user notification permissions configured. The database test will verify delivery.";
                busy=false;DialogResult=DialogResult.OK;Close();
            }
            catch(Exception ex)
            {
                if(ex is Microsoft.Data.SqlClient.SqlException{Number:51120})authentication.SelectedIndex=1;
                status.Text=ex.Message+" Use administrator credentials if the configured login lacks permission.";
            }
            finally{password.Clear();busy=false;execute.Enabled=close.Enabled=authentication.Enabled=true;RefreshAuthentication();UseWaitCursor=false;}
        };
    }
}
