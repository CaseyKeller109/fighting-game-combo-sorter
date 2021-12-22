using System;
using System.IO;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace ComboSorter
{
    public partial class Form1 : Form
    {
        //todo show all attributes of found combos

        SeriesWithCharas currentGame;
        Boolean isPreparingEditMode = false;
        Boolean isEditMode;

        int currentRow = 0;

        string currentFoundInputs = "";
        string currentFoundTags = "";
        string currentChara = "";
        string currentTags = "";

        int tester = 0;

        List<List<RadioButton>> radioListList = new List<List<RadioButton>>();

        private static System.Data.SqlClient.SqlConnection con = new System.Data.SqlClient.SqlConnection();

        public class SeriesWithCharas
        {
            public SeriesWithCharas() { }
            public SeriesWithCharas(string seriesNameIn, List<string> characterNameIn)
            {
                string seriesName = seriesNameIn;

                List<string> characterNames = characterNameIn;
            }

            public string gameName = "not set";
            public List<string> characterNames;
        }

        public Form1()
        {
            InitializeComponent();

            ToSearchMode();

            errormessage.Text = "";

            dataGridView1.AllowUserToAddRows = false;

            greaterlessequalreps.SelectedIndex = 0;
            greaterlessequalmeter.SelectedIndex = 0;
            greaterlessequaldamage.SelectedIndex = 0;

            dataGridView1.CellValueChanged -= dataGridView1_CellValueChanged;
            dataGridView1.CellValueChanged += dataGridView1_CellValueChanged;
            dataGridView1.CellClick -= dataGridView1_CellClick;
            dataGridView1.CellClick += dataGridView1_CellClick;

            radioListList = new List<List<RadioButton>>() {
                new List<RadioButton>() {groundtoground, groundtoair, airtoair, airtoground, instantairdash, lowairdash }
                , new List<RadioButton>() {midscreen, corner }
                , new List<RadioButton>() {closerange, midrange, farrange }
                , new List<RadioButton>() { combo, blockstring, setup }
                , new List<RadioButton>() {highattack, midattack, lowattack }
                , new List<RadioButton>() { okiyes, okino }
                , new List<RadioButton>() {counteryes, counterno }
                , new List<RadioButton>() {sameside, sideswitch }
                , new List<RadioButton>() {throwyes, throwno }
                , new List<RadioButton>() {throwosyes, throwosno } };

            foreach (List<RadioButton> buttonList in radioListList)
            {
                foreach (RadioButton button in buttonList)
                {
                    button.Click += searchbutton_Click;
                }
            }

            List<RadioButton> anyRadioList = new List<RadioButton>() { any1, any2, any3, any4, any5, any6, any7, any8, any9, any10 };
            foreach (RadioButton button in anyRadioList)
            {
                button.Click += searchbutton_Click;
            }

            string baseDirectory = Directory.GetParent(Environment.CurrentDirectory).Parent.FullName;
            con.ConnectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;"
                        +
                        @"AttachDbFilename=" + baseDirectory + @"\ComboSorterDatabase.mdf;"
                        + @"Integrated Security=True;
                        Connect Timeout=3";

            PopulateSeriesButtons("");
        }

        private void PopulateSeriesButtons(string searchTerm)
        {

            //todo don't instantiate list every time it populates
            List<SeriesWithCharas> gameList = new List<SeriesWithCharas>() {
                new SeriesWithCharas(){gameName = "Guilty Gear Rev 2", characterNames =  new List<string>() { "Answer", "Axl Low", "Baiken", "Bedman", "Chipp Zanuff", "Dizzy", "Elphelt Valentine", "Faust", "I-No", "Jack-O'", "Jam Kuradoberi", "Johnny", "Kum Haehyun", "Ky Kiske", "Leo Whitefang", "May", "Millia Rage", "Potemkin", "Ramlethal Valentine", "Raven", "Sin Kiske", "Slayer", "Sol Badguy", "Venom", "Zato-1" }  }
                , new SeriesWithCharas(){gameName = "Super Street Fighter 2 Turbo", characterNames =  new List<string>() { "Akuma", "Balrog", "Blanka", "Cammy", "Chun-Li", "Dee Jay", "Dhalsim", "E-Honda", "Fei Long", "Guile", "Ken", "M. Bison", "Ryu", "Sagat", "T. Hawk", "Vega", "Zangief" }  } 
            };

            SeriesPanel.Controls.Clear();

            foreach (SeriesWithCharas game in gameList)
            {
                if (game.gameName.ToLower().Contains(searchTerm.ToLower()))
                {
                    Button newButton = new Button();
                    newButton.AutoSize = true;
                    newButton.Click += (sender2, e) => SeriesButtonClick(sender2, e, game);

                    newButton.Text = game.gameName;

                    SeriesPanel.Controls.Add(newButton);
                }
            }
        }

        private void PopulateCharaButtons(SeriesWithCharas game, string searchTerm)
        {
            currentChara = "";
            CharaPanel.Controls.Clear();

            foreach (string chara in game.characterNames)
            {
                if (chara.ToLower().Contains(searchTerm.ToLower()))
                {
                    Button newButton = new Button();
                    newButton.AutoSize = true;

                    newButton.Text = chara;

                    newButton.Click += (sender2, e2) => CharaButtonClick(sender2, e2, chara);
                    CharaPanel.Controls.Add(newButton);
                }
            }
        }

        private void SeriesButtonClick(object sender, EventArgs e, SeriesWithCharas game)
        {
            dataGridView1.Rows.Clear();

            foreach (Button butt in SeriesPanel.Controls)
            {
                butt.BackColor = default(Color);
            }
            Control ctrl = (Control)sender;
            ctrl.BackColor = Color.DarkGray;

            currentGame = game;
            PopulateCharaButtons(currentGame, charasearch.Text);

        }

        private void CharaButtonClick(object sender, EventArgs e, String chara)
        {
            foreach (Button butt in CharaPanel.Controls)
            {
                butt.BackColor = default(Color);
            }
            Control ctrl = (Control)sender;
            ctrl.BackColor = Color.DarkGray;

            currentChara = chara;
            GetCombosFromDatabase();
        }

        //returns false if tag(s) missing 
        private Boolean GetCurrentTags(Boolean anyAllowed)
        {
            currentTags = "";

            foreach (List<RadioButton> radioList in radioListList)
            {
                Boolean isATagChecked = false;
                foreach (RadioButton tag in radioList)
                {
                    if (tag.Checked)
                    {
                        currentTags += tag.Name + " ";
                        isATagChecked = true;
                    }
                }
                if (!isATagChecked && !anyAllowed)
                {
                    return false;
                }
            }
            return true;
        }

        //returns false if can't get them
        private void GetCombosFromDatabase()
        {
            //todo check if this breaks anything???
            if (isEditMode == true || isPreparingEditMode == true) { return; }
            //label1.Text += "a";
            //return;
            //if (currentGame == default(SeriesWithCharas) || currentChara == "") { return false; }

            //GetCombosFromDatabase(object sender, EventArgs e)
            GetCurrentTags(true);
            //if (GetCurrentTags() == false) { return false; }

            //Next Already Scored

            //todo implement AND and OR for entire command, and substitute AND for spaces between terms
            string searchCommand;



            if (currentGame != default(SeriesWithCharas) && currentChara != "")
            {
                searchCommand = $" WHERE series LIKE '%{currentGame.gameName}%' AND chara LIKE '%{currentChara}%'";
            }
            else
            {
                return;
            }

            //todo replace enterlanguagetextbox.Text, etc with filterDictionary[language].textBox?

            if (inputsearchstart.Text != "")
            {
                searchCommand += $" AND inputs LIKE '{inputsearchstart.Text}%'";
            }
            if (inputsearchend.Text != "")
            {
                searchCommand += $" AND inputs LIKE '%{inputsearchend.Text}'";
            }
            if (inputsearchinclude.Text != "")
            {
                searchCommand += $" AND inputs LIKE '%{inputsearchinclude.Text}%'";
            }

            if (maxreps.Text.ToString() != "")
            {
                if (greaterlessequalreps.SelectedIndex == 0)
                {
                    searchCommand += $" AND maxreps > {maxreps.Text.ToString()}";
                }
                else if (greaterlessequalreps.SelectedIndex == 1)
                {
                    searchCommand += $" AND maxreps < {maxreps.Text.ToString()}";
                }
                else if (greaterlessequalreps.SelectedIndex == 2)
                {
                    searchCommand += $" AND maxreps = {maxreps.Text.ToString()}";
                }
            }

            if (meter.Text.ToString() != "")
            {
                if (greaterlessequalmeter.SelectedIndex == 0)
                {
                    searchCommand += $" AND meter > {meter.Text.ToString()}";
                }
                else if (greaterlessequalmeter.SelectedIndex == 1)
                {
                    searchCommand += $" AND meter < {meter.Text.ToString()}";
                }
                else if (greaterlessequalmeter.SelectedIndex == 2)
                {
                    searchCommand += $" AND meter = {meter.Text.ToString()}";
                }
            }

            if (damage.Text.ToString() != "")
            {
                if (greaterlessequaldamage.SelectedIndex == 0)
                {
                    searchCommand += $" AND damage > {damage.Text.ToString()}";
                }
                else if (greaterlessequaldamage.SelectedIndex == 1)
                {
                    searchCommand += $" AND damage < {damage.Text.ToString()}";
                }
                else if (greaterlessequaldamage.SelectedIndex == 2)
                {
                    searchCommand += $" AND damage = {damage.Text.ToString()}";
                }
            } 

            if (currentTags != "")
            {
                string[] tagArray = currentTags.Trim().Split();

                foreach (string tag in tagArray)
                {
                    searchCommand += $" AND tags LIKE '%{tag}%'";
                }
            }

            using (SqlCommand command = new SqlCommand($"SELECT * FROM ComboTable" + searchCommand, con))
            {
                con.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    dataGridView1.Rows.Clear();
                    do
                    {
                        while (reader.Read())
                        {
                            Console.WriteLine("\t{0}\t{1}", reader.GetInt32(0),
                                reader.GetString(1));
                            label1.Text = reader.GetString(2);

                            dataGridView1.Rows.Add(reader.GetString(3), reader.GetInt32(6).ToString(),
                                reader.GetInt32(4).ToString(), reader.GetString(7), reader.GetInt32(5).ToString());

                            label1.Text += reader.GetInt32(0);
                        }
                    }
                    while (reader.NextResult());
                }
                con.Close();
            }

            if (dataGridView1.CurrentRow == null)
            {
                currentFoundInputs = "";
                currentFoundTags = "";
            }

            else
            {
                currentFoundInputs = dataGridView1.CurrentRow.Cells[0].Value.ToString();
                currentFoundTags = dataGridView1.CurrentRow.Cells[3].Value.ToString();
            }
            label1.Text = currentFoundInputs;
        }

        private void AddComboToDatabase()
        {
            //todo add error text if missing required data
            if (currentGame == default(SeriesWithCharas)) { errormessage.Text = "Select Series and Character!"; return; }
            else if (currentChara == "") { errormessage.Text = "Select Character!"; return; }
            else if (GetCurrentTags(false) == false) { errormessage.Text = "Select All Tags!"; return; }
            else if (newcombobox.Text == "") { errormessage.Text = "Enter Combo!"; return; }
            else if (maxreps.Text == "") { errormessage.Text = "Enter Max Reps!"; return; }
            else if (meter.Text == "") { errormessage.Text = "Enter Meter!"; return; }
            else if (damage.Text == "") { errormessage.Text = "Enter Damage!"; return; }
            else

            {
                con.Open();
                string sql = $"INSERT INTO ComboTable(series, chara, inputs, damage, meter, maxreps, tags) " +
                    $"VALUES (@series,@chara,@inputs,@damage,@meter,@maxreps,@tags)";
                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.Add("@series", SqlDbType.VarChar).Value = currentGame.gameName;
                    cmd.Parameters.Add("@chara", SqlDbType.VarChar).Value = currentChara;
                    cmd.Parameters.Add("@inputs", SqlDbType.VarChar).Value = newcombobox.Text.ToString();
                    cmd.Parameters.Add("@damage", SqlDbType.Int).Value = Convert.ToInt32(damage.Text.ToString());
                    cmd.Parameters.Add("@meter", SqlDbType.Int).Value = Convert.ToInt32(meter.Text.ToString());
                    cmd.Parameters.Add("@maxreps", SqlDbType.Int).Value = Convert.ToInt32(maxreps.Text.ToString());
                    cmd.Parameters.Add("@tags", SqlDbType.VarChar).Value = currentTags;

                    cmd.CommandType = CommandType.Text;
                    cmd.ExecuteNonQuery();
                }
                con.Close();
            }

            GetCombosFromDatabase();
            //todo use these???
            //maxreps.Text = "";
            //meter.Text = "";
            //damage.Text = "";
            //newcombobox.Text = "";
            ShowErrorMessage("Combo Added!");

        }

        private void UpdateCombo()
        {
            label1.Text = "updating!!!";

            GetCurrentTags(false);

            con.Open();

            string sql = $"UPDATE ComboTable SET tags = @tags"
            + $" WHERE inputs = @inputs"
            + $" AND chara = @chara";
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.Add("@tags", SqlDbType.VarChar).Value = currentTags;
                cmd.Parameters.Add("@inputs", SqlDbType.VarChar).Value = dataGridView1.CurrentRow.Cells[0].Value;
                cmd.Parameters.Add("@chara", SqlDbType.VarChar).Value = currentChara;

                cmd.CommandType = CommandType.Text;
                cmd.ExecuteNonQuery();
            }
            con.Close();

            dataGridView1.CurrentRow.Cells[3].Value = currentTags;

            currentFoundTags = currentTags;

            GetCombosFromDatabase();
        }

        private void DeleteCombo()
        {
            con.Open();
            string sql = $"DELETE FROM ComboTable WHERE inputs = @inputs OR inputs = ''"
                + $" AND chara = @chara"
                ;
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.Add("@inputs", SqlDbType.VarChar).Value = dataGridView1.CurrentRow.Cells[0].Value;
                cmd.Parameters.Add("@chara", SqlDbType.VarChar).Value = currentChara;

                cmd.CommandType = CommandType.Text;
                cmd.ExecuteNonQuery();
            }
            con.Close();

            //todo don't do sql query to update here
            GetCombosFromDatabase();
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {

            if (dataGridView1.CurrentCell.ColumnIndex == 0)
            {
                con.Open();
                string sql = $"UPDATE ComboTable SET inputs = @inputs "
                    + $" WHERE inputs = @oldinputs OR inputs = ''"
                    + $" AND chara = @chara";
                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.Add("@inputs", SqlDbType.VarChar).Value = dataGridView1.CurrentRow.Cells[0].Value.ToString();
                    cmd.Parameters.Add("@oldinputs", SqlDbType.VarChar).Value = currentFoundInputs;
                    cmd.Parameters.Add("@chara", SqlDbType.VarChar).Value = currentChara;

                    cmd.CommandType = CommandType.Text;
                    cmd.ExecuteNonQuery();
                }
                con.Close();
            }
            else if (dataGridView1.CurrentCell.ColumnIndex == 1)
            {
                con.Open();
                string sql = $"UPDATE ComboTable SET maxreps = @maxreps"
                    + $" WHERE inputs = @inputs"
                    + $" AND chara = @chara";
                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.Add("@maxreps", SqlDbType.Int).Value = dataGridView1.CurrentCell.Value;
                    cmd.Parameters.Add("@inputs", SqlDbType.VarChar).Value = dataGridView1.CurrentRow.Cells[0].Value.ToString();
                    cmd.Parameters.Add("@chara", SqlDbType.VarChar).Value = currentChara;
                    cmd.CommandType = CommandType.Text;
                    cmd.ExecuteNonQuery();
                }
                con.Close();
            }
            else if (dataGridView1.CurrentCell.ColumnIndex == 2)
            {
                con.Open();
                string sql = $"UPDATE ComboTable SET damage = @damage"
                    + $" WHERE inputs = @inputs"
                    + $" AND chara = @chara";

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.Add("@inputs", SqlDbType.VarChar).Value = dataGridView1.CurrentRow.Cells[0].Value.ToString();
                    cmd.Parameters.Add("@chara", SqlDbType.VarChar).Value = currentChara;
                    cmd.Parameters.Add("@damage", SqlDbType.Int).Value = dataGridView1.CurrentCell.Value;

                    cmd.CommandType = CommandType.Text;
                    cmd.ExecuteNonQuery();
                }
                con.Close();
            }
            else if (dataGridView1.CurrentCell.ColumnIndex == 3)
            {
                con.Open();
                string sql = $"UPDATE ComboTable SET meter = @meter"
                    + $" WHERE inputs = @inputs"
                    + $" AND chara = @chara";
                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.Add("@inputs", SqlDbType.VarChar).Value = dataGridView1.CurrentRow.Cells[0].Value.ToString();
                    cmd.Parameters.Add("@chara", SqlDbType.VarChar).Value = currentChara;
                    cmd.Parameters.Add("@meter", SqlDbType.Int).Value = dataGridView1.CurrentCell.Value;

                    cmd.CommandType = CommandType.Text;
                    cmd.ExecuteNonQuery();
                }
                con.Close();
            }

        }

        private void SetRadiosToFirst()
        {
            foreach (List<RadioButton> buttonList in radioListList)
            {
                buttonList[0].Checked = true;

                maxreps.Text = "3";
                meter.Text = "5";
                damage.Text = "4";
            }
        }

        private void ResetRadiosToAny()
        {
            List<RadioButton> anyRadioList = new List<RadioButton>() { any1, any2, any3, any4, any5, any6, any7, any8, any9, any10 };
            foreach (RadioButton button in anyRadioList)
            {
                button.Checked = true;
            }
            maxreps.Text = "";
            meter.Text = "";
            damage.Text = "";
        }

        private void dataGridView1_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            currentFoundInputs = dataGridView1.CurrentRow.Cells[0].ToString();

            label1.Text = currentFoundInputs;
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (currentRow == dataGridView1.CurrentCell.RowIndex) { return; }

            currentRow = dataGridView1.CurrentCell.RowIndex;

            currentFoundInputs = dataGridView1.CurrentRow.Cells[0].Value.ToString();
            currentFoundTags = dataGridView1.CurrentRow.Cells[3].Value.ToString();

            if (isEditMode)
            {
                SetAttributesToCurrentSelected();
            }

            tester += 1;
            label1.Text = (tester).ToString();
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {


        }

        private void farrange_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void addbutton_Click(object sender, EventArgs e)
        {
            AddComboToDatabase();
            //GetCombosFromDatabase();
            //damage.Text = "";
            //newcombobox.Text = "";
        }

        private void searchbutton_Click(object sender, EventArgs e)
        {
            SearchOrEdit();

        }

        private void button1_Click(object sender, EventArgs e)
        {
            DeleteCombo();
        }

        private void SeriesSearch_TextChanged(object sender, EventArgs e)
        {
            PopulateSeriesButtons(seriessearch.Text);
        }

        private void charasearch_TextChanged(object sender, EventArgs e)
        {
            PopulateCharaButtons(currentGame, charasearch.Text);
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            GetCombosFromDatabase();
        }

        private void label13_Click(object sender, EventArgs e)
        {

        }

        private void maxreps_TextChanged(object sender, EventArgs e)
        {
            if (!isEditMode) { GetCombosFromDatabase(); return; }

            con.Open();
                string sql = $"UPDATE ComboTable SET maxreps = @maxreps"
                    + $" WHERE inputs = @inputs"
                    + $" AND chara = @chara";
                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.Add("@maxreps", SqlDbType.Int).Value = dataGridView1.CurrentCell.Value;
                    cmd.Parameters.Add("@inputs", SqlDbType.VarChar).Value = dataGridView1.CurrentRow.Cells[0].Value.ToString();
                    cmd.Parameters.Add("@chara", SqlDbType.VarChar).Value = currentChara;
                    cmd.CommandType = CommandType.Text;
                    cmd.ExecuteNonQuery();
                }
                con.Close();
        }

        private void maxreps_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }

        }

        private void meter_SelectedIndexChanged(object sender, EventArgs e)
        {
            GetCombosFromDatabase();
        }

        private void greaterlessequaldamage_SelectedIndexChanged(object sender, EventArgs e)
        {
            GetCombosFromDatabase();
        }

        private void damage_TextChanged(object sender, EventArgs e)
        {
            if (!isEditMode) { GetCombosFromDatabase(); return; }

            con.Open();
                string sql = $"UPDATE ComboTable SET damage = @damage"
                    + $" WHERE inputs = @inputs"
                    + $" AND chara = @chara";
                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.Add("@inputs", SqlDbType.VarChar).Value = dataGridView1.CurrentRow.Cells[0].Value.ToString();
                    cmd.Parameters.Add("@chara", SqlDbType.VarChar).Value = currentChara;
                    cmd.Parameters.Add("@damage", SqlDbType.Int).Value = dataGridView1.CurrentCell.Value;
                    cmd.CommandType = CommandType.Text;
                    cmd.ExecuteNonQuery();
                }
                con.Close();
        }

        private void inputsearchstart_TextChanged(object sender, EventArgs e)
        {
            GetCombosFromDatabase();
        }

        private void inputsearchend_TextChanged(object sender, EventArgs e)
        {
            GetCombosFromDatabase();
        }

        private void resetfilter_Click(object sender, EventArgs e)
        {
            ResetRadiosToAny();
            GetCombosFromDatabase();
        }

        private void autofilter_Click(object sender, EventArgs e)
        {
            SetRadiosToFirst();
        }

        private void meter_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void meter_TextChanged(object sender, EventArgs e)
        {
            if (!isEditMode) { GetCombosFromDatabase();  return; }

            con.Open();
                string sql = $"UPDATE ComboTable SET meter = @meter"
                    + $" WHERE inputs = @inputs"
                    + $" AND chara = @chara";
                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.Add("@inputs", SqlDbType.VarChar).Value = dataGridView1.CurrentRow.Cells[0].Value.ToString();
                    cmd.Parameters.Add("@chara", SqlDbType.VarChar).Value = currentChara;
                    cmd.Parameters.Add("@meter", SqlDbType.Int).Value = dataGridView1.CurrentCell.Value;
                    cmd.CommandType = CommandType.Text;
                    cmd.ExecuteNonQuery();
                }
                con.Close();
        }

        private void greaterlessequalreps_SelectedIndexChanged(object sender, EventArgs e)
        {
            GetCombosFromDatabase();
        }

        private void greaterlessequalmeter_SelectedIndexChanged(object sender, EventArgs e)
        {
            GetCombosFromDatabase();
        }

        private void searchmodebutton_Click(object sender, EventArgs e)
        {

            ToSearchMode();
        }

        private void editmodebutton_Click(object sender, EventArgs e)
        {
            ToEditMode();
        }

        private void ToSearchMode()
        {
            //dataGridView1.Rows.Clear();
            //GetCombosFromDatabase();

            //foreach (Button butt in SeriesPanel.Controls)
            //{
            isEditMode = false;
            editmodebutton.BackColor = default(Color);
            //}

            searchmodebutton.BackColor = Color.DarkGray;
            //selectedGame = game;
            //label1.Text = game.gameName;
            //label1.Text = game.characterNames[0];

            //currentGame = game;
            //PopulateCharaButtons(currentGame, charasearch.Text);
        }

        private void ToEditMode()
        {
            isPreparingEditMode = true;

            if (currentFoundInputs == "") { ShowErrorMessage("Please select combo to edit!"); return; }


            errormessage.Text = "";

            searchmodebutton.BackColor = default(Color);

            editmodebutton.BackColor = Color.DarkGray;



            currentFoundTags = dataGridView1.CurrentRow.Cells[3].Value.ToString();

            SetAttributesToCurrentSelected();


            isEditMode = true;
            isPreparingEditMode = false;

        }

        private void SetAttributesToCurrentSelected()
        {
            currentTags = currentFoundTags;

            maxreps.Text = dataGridView1.CurrentRow.Cells[1].Value.ToString();
            //meter.Text = "aaa";
            meter.Text = dataGridView1.CurrentRow.Cells[4].Value.ToString();
            damage.Text = dataGridView1.CurrentRow.Cells[2].Value.ToString();

            foreach (List<RadioButton> radioList in radioListList)
            {
                foreach (RadioButton tag in radioList)
                {
                    if (currentFoundTags.Contains(tag.Name))
                    {
                        tag.Checked = true;
                    }
                }
            }
        }

        private void ShowErrorMessage(string error)
        {
            errormessage.Text = error;
            Task.Factory.StartNew(() =>
                    {
                        Thread.Sleep(2000);
                        errormessage.Text = "";
                    });
        }

        private void SearchOrEdit()
        {
            if (isEditMode) { UpdateCombo(); }
            else if (!isEditMode) { GetCombosFromDatabase(); }
        }

        private void newcombobox_TextChanged(object sender, EventArgs e)
        {
            //todo make method for all these
            if (!isEditMode) { return; }
            con.Open();
            string sql = $"UPDATE ComboTable SET inputs = @inputs "
                + $" WHERE inputs = @oldinputs OR inputs = ''"
                + $" AND chara = @chara";
            using (SqlCommand cmd = new SqlCommand(sql, con))
            {
                cmd.Parameters.Add("@inputs", SqlDbType.VarChar).Value = dataGridView1.CurrentRow.Cells[0].Value.ToString();
                cmd.Parameters.Add("@oldinputs", SqlDbType.VarChar).Value = currentFoundInputs;
                cmd.Parameters.Add("@chara", SqlDbType.VarChar).Value = currentChara;

                cmd.CommandType = CommandType.Text;
                cmd.ExecuteNonQuery();
            }
            con.Close();

        }
    }
}
