using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Button = System.Windows.Forms.Button;
using Control = System.Windows.Controls.Control;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using ListBox = System.Windows.Controls.ListBox;
using MessageBox = System.Windows.Forms.MessageBox;
using TextBox = System.Windows.Forms.TextBox;

namespace CodexLite.Apps
{
    /// <summary>
    /// Todo.xaml 的交互逻辑
    /// </summary>
    public partial class Todo : Page
    {
        private static SqliteLink _sqliteLink;
        private static BindingSource _groupModelBindingSource;
        private static BindingSource _eventModelBindingSource;

        public Todo()
        {
            InitializeComponent();
            _sqliteLink = new SqliteLink();
            _groupModelBindingSource = new BindingSource();
            _eventModelBindingSource = new BindingSource();

            _groupModelBindingSource.DataSource = GroupDao.QueryAllGroupModels(_sqliteLink);
            GroupListBox.ItemsSource = _groupModelBindingSource;
        }

        ~Todo()
        {
            try
            {
                _sqliteLink.Close();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void UpperListBoxSelected_OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            if (GroupListBox.SelectedItem != null && UpperListBox.SelectedItem != null)
            {
                GroupListBox.SelectedItem = null;
            }

            ListBoxItem selectedItem = (ListBoxItem)UpperListBox.SelectedItem;
            if (selectedItem != null && (string)selectedItem.DataContext == "0")
            {
                EventToolbarContentControl.ContentTemplate = Resources["ImportantToolbarTemplate"] as DataTemplate;
                NewEventBorder.Visibility = Visibility.Visible;
                NewEventButton.DataContext = new GroupModel("0", "重要", 0);
                _eventModelBindingSource.DataSource =
                    EventDao.QueryEventModelsByGroupIndex(_sqliteLink, NewEventButton.DataContext as GroupModel);
                EventListBox.ItemsSource = _eventModelBindingSource;
            }
            else if (selectedItem != null && (string)selectedItem.DataContext == "1")
            {
                EventToolbarContentControl.ContentTemplate = Resources["TaskToolbarTemplate"] as DataTemplate;
                NewEventBorder.Visibility = Visibility.Visible;
                NewEventButton.DataContext = new GroupModel("1", "任务", 0);
                _eventModelBindingSource.DataSource =
                    EventDao.QueryEventModelsByGroupIndex(_sqliteLink, NewEventButton.DataContext as GroupModel);
                EventListBox.ItemsSource = _eventModelBindingSource;
            }
            else
            {
                EventToolbarContentControl.ContentTemplate = null;
                NewEventBorder.Visibility = Visibility.Hidden;
                NewEventButton.DataContext = null;
                EventListBox.ItemsSource = null;
            }

        }

        private void GroupIndexListBoxSelected_OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            if (UpperListBox.SelectedItem != null && GroupListBox.SelectedItem != null)
            {
                UpperListBox.SelectedItem = null;
            }

            if (GroupListBox.SelectedItem != null)
            {
                EventToolbarContentControl.ContentTemplate = Resources["GroupToolbarTemplate"] as DataTemplate;
                NewEventBorder.Visibility = Visibility.Visible;
                NewEventButton.DataContext = GroupListBox.SelectedItem;
                _eventModelBindingSource.DataSource =
                    EventDao.QueryEventModelsByGroupIndex(_sqliteLink, NewEventButton.DataContext as GroupModel);
                EventListBox.ItemsSource = _eventModelBindingSource;
            }
            else
            {
                EventToolbarContentControl.ContentTemplate = null;
                NewEventBorder.Visibility = Visibility.Hidden;
                NewEventButton.DataContext = null;
                EventListBox.ItemsSource = null;
            }
        }

        private void EventListBoxSelected_OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            if (EventListBox.SelectedItem != null)
            {
                var rightBorderAnimation = Resources["RightBorderExtendStoryboard"] as Storyboard;
                rightBorderAnimation?.Begin();
                RightGrid.DataContext = EventListBox.SelectedItem;

            }
            else
            {
                var rightBorderAnimation = Resources["RightBorderNarrowStoryboard"] as Storyboard;
                rightBorderAnimation?.Begin();
                RightGrid.DataContext = null;
            }
        }

        private void InsertNewGroup_OnClick(object sender, RoutedEventArgs e)
        {
            if (NewGroupTextBox.Text == "") return;


            string timeStamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds().ToString();
            GroupModel newGroupModel = new GroupModel(timeStamp, NewGroupTextBox.Text);
            try
            {
                GroupDao.InsertNewGroup(_sqliteLink, newGroupModel);
            }
            catch (Exception)
            {
                return;
            }

            _groupModelBindingSource.Add(newGroupModel);
            NewGroupTextBox.Clear();
        }

        private void InsertNewGroup_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (NewGroupTextBox.Text == "" || e.Key != Key.Enter) return;

            string timeStamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds().ToString();
            GroupModel newGroupModel = new GroupModel(timeStamp, NewGroupTextBox.Text);
            try
            {
                GroupDao.InsertNewGroup(_sqliteLink, newGroupModel);
            }
            catch (Exception)
            {
                return;
            }

            _groupModelBindingSource.Add(newGroupModel);
            NewGroupTextBox.Clear();
        }

        private void DeleteGroup_OnClick(object sender, RoutedEventArgs e)
        {
            var button = (System.Windows.Controls.Button)sender;
            List<string> eventIndexes =
                GroupEventDao.QueryAllEventIndexesByGroupIndex(_sqliteLink, button.DataContext as GroupModel);
            GroupEventDao.DeleteAllGroupEventRelationsByGroupIndex(_sqliteLink, button.DataContext as GroupModel);
            List<string> deleteEventIndexes =
                GroupEventDao.CheckEventIndexesExistsByEventIndex(_sqliteLink, eventIndexes);
            foreach (var deleteEventIndex in deleteEventIndexes)
            {
                EventDao.DeleteEventModelByEventIndex(_sqliteLink, deleteEventIndex);
            }
            GroupDao.DeleteGroup(_sqliteLink, button.DataContext as GroupModel);
            if (button.DataContext == GroupListBox.SelectedItem)
            {
                GroupListBox.SelectedItem = null;
            }
            try
            {
                
            }
            catch (Exception)
            {
                return;
            }

            _groupModelBindingSource.DataSource = GroupDao.QueryAllGroupModels(_sqliteLink);

        }

        private void InsertNewEvent_OnClick(object sender, RoutedEventArgs e)
        {
            if (NewEventTextBox.Text == "") return;
            string timeStamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds().ToString();
            EventModel newEventModel = new EventModel(timeStamp, NewEventTextBox.Text, DateTime.Now);
            try
            {
                EventDao.InsertNewEvent(_sqliteLink, newEventModel);
                try
                {
                    var button = (System.Windows.Controls.Button)sender;
                    GroupEventModel newGroupEventModel =
                        new GroupEventModel((GroupModel)button.DataContext, newEventModel);
                    GroupEventDao.InsertNewGroupEventRelation(_sqliteLink, newGroupEventModel);
                }
                catch (Exception)
                {
                    return;
                }
            }
            catch (Exception)
            {
                return;
            }

            _eventModelBindingSource.Add(newEventModel);
            NewEventTextBox.Clear();
        }

        private void InsertNewEvent_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (NewEventTextBox.Text == "" || e.Key != Key.Enter) return;
            string timeStamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds().ToString();
            EventModel newEventModel = new EventModel(timeStamp, NewEventTextBox.Text, DateTime.Now);
            try
            {
                EventDao.InsertNewEvent(_sqliteLink, newEventModel);
                try
                {
                    GroupEventModel newGroupEventModel =
                        new GroupEventModel((GroupModel)NewEventButton.DataContext, newEventModel);
                    GroupEventDao.InsertNewGroupEventRelation(_sqliteLink, newGroupEventModel);
                }
                catch (Exception)
                {
                    return;
                }
            }
            catch (Exception)
            {
                return;
            }

            _eventModelBindingSource.Add(newEventModel);
            NewEventTextBox.Clear();
        }

        private void ChangeEventCompletion_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button)
            {
                if (button.DataContext is EventModel eventModel)
                {
                    eventModel.EventCompletion = eventModel.EventCompletion == "0" ? "1" : "0";
                    EventDao.UpdateEventModel(_sqliteLink, eventModel);
                }

                _eventModelBindingSource.DataSource =
                    EventDao.QueryEventModelsByGroupIndex(_sqliteLink, NewEventButton.DataContext as GroupModel);
            }
        }

        private void UpdateEventModel_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Button button)
                    EventDao.UpdateEventModel(_sqliteLink, button.DataContext as EventModel);
            }
            catch (Exception)
            {
                // Pass;
            }
        }

        private void AbandonChange_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button)
            {
                try
                {
                    EventListBox.SelectedItem = null;
                    _eventModelBindingSource.DataSource =
                        EventDao.QueryEventModelsByGroupIndex(_sqliteLink, NewEventButton.DataContext as GroupModel);
                }
                catch (Exception)
                {
                    //Pass
                }
            }
                
        }

        private void NarrowRightBorder_OnClick(object sender, RoutedEventArgs e)
        {
            var rightBorderAnimation = Resources["RightBorderNarrowStoryboard"] as Storyboard;
            rightBorderAnimation?.Begin();
            EventListBox.SelectedItem = null;
        }

        private void DeleteEventModel_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button)
            {
                if (button.DataContext is EventModel eventModel && NewEventButton.DataContext is GroupModel groupModel)
                {
                    GroupEventDao.DeleteGroupEventRelationByGroupIndexAndEventIndex(_sqliteLink,
                            groupModel.GroupIndex, eventModel.EventIndex);
                    List<string> eventIndexes = new List<string> { eventModel.EventIndex };
                    List<string> deleteEventIndexes =
                        GroupEventDao.CheckEventIndexesExistsByEventIndex(_sqliteLink, eventIndexes);
                    if (deleteEventIndexes.Count != 0)
                    {
                        EventDao.DeleteEventModelByEventIndex(_sqliteLink, eventModel.EventIndex);
                        _eventModelBindingSource.DataSource =
                            EventDao.QueryEventModelsByGroupIndex(_sqliteLink, groupModel);
                        EventListBox.SelectedItem = null;
                    }
                }

            }
            
        }
    }

    [ValueConversion(typeof(DateTime), typeof(string))]
    internal class EventStartDateConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                string dateTime = ((DateTime)value).ToString("yyyy-MM-dd");
                return dateTime == DateTime.Today.ToString("yyyy-MM-dd") ? "创建于 今天" :
                    dateTime == DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd") ? "创建于 昨天" :
                    "创建于 " + dateTime;
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(DateTime), typeof(string))]
    internal class EventEndDateDisplayConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                string dateTime = ((DateTime)value).ToString("yyyy-MM-dd");
                var t = dateTime == DateTime.Today.ToString("yyyy-MM-dd") ? "今天" :
                    dateTime == DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd") ? "昨天" :
                    dateTime == DateTime.Today.AddDays(1).ToString("yyyy-MM-dd") ? "明天" : dateTime;
                return t;
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(DateTime), typeof(string))]
    internal class EventEndDateDatabaseConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((DateTime?)value)?.ToString("yyyy-MM-dd");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                DateTimeFormatInfo dateTimeFormatInfo = new DateTimeFormatInfo()
                {
                    ShortDatePattern = "yyyy-MM-dd"
                };
                return System.Convert.ToDateTime(value as string, dateTimeFormatInfo);
            }

            return null;
        }
    }

    internal class SqliteLink
    {
        private readonly SQLiteConnection _sqliteConnection;

        public SqliteLink()
        {
            _sqliteConnection = new SQLiteConnection("Data Source=CodexLite.sqlite;Version=3;");
        }

        public void Open()
        {
            _sqliteConnection.Open();
        }

        public void Close()
        {
            _sqliteConnection.Close();
        }

        public SQLiteConnection GetConnection()
        {
            return _sqliteConnection;
        }
    }

    internal class GroupModel
    {
        public GroupModel(string groupIndex, string groupName, long editable = 1)
        {
            GroupIndex = groupIndex;
            GroupName = groupName;
            Editable = editable;
        }

        public string GroupIndex { get; set; }
        public string GroupName { get; set; }
        public long Editable { get; set; }
    }

    internal static class GroupDao
    {
        public static List<GroupModel> QueryAllGroupModels(SqliteLink sqliteLink)
        {
            sqliteLink.Open();
            string sql = "SELECT * FROM 'group' WHERE editable=1";
            List<GroupModel> groupIndexModels = new List<GroupModel>();
            SQLiteCommand sqliteCommand = new SQLiteCommand(sql, sqliteLink.GetConnection());
            using (var reader = sqliteCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    GroupModel groupModel = new GroupModel((string)reader["group_index"],
                        (string)reader["group_name"], (long)reader["editable"]);
                    groupIndexModels.Add(groupModel);
                }
            }

            sqliteLink.Close();
            return groupIndexModels;
        }

        public static void InsertNewGroup(SqliteLink sqliteLink, GroupModel groupModel)
        {
            sqliteLink.Open();
            string sql =
                @"INSERT INTO 'group'(group_index, group_name, editable) VALUES ($index, $name, $editable)";
            SQLiteCommand sqLiteCommand = new SQLiteCommand();
            sqLiteCommand.Connection = sqliteLink.GetConnection();
            sqLiteCommand.CommandText = sql;
            sqLiteCommand.Parameters.Add("$index", DbType.String).Value = groupModel.GroupIndex;
            sqLiteCommand.Parameters.Add("$name", DbType.String).Value = groupModel.GroupName;
            sqLiteCommand.Parameters.Add("$editable", DbType.Int64).Value = groupModel.Editable;
            try
            {
                sqLiteCommand.ExecuteNonQuery();
            }
            catch (Exception)
            {
                sqliteLink.Close();
                throw;
            }

            sqliteLink.Close();
        }

        public static void DeleteGroup(SqliteLink sqliteLink, GroupModel groupModel)
        {
            sqliteLink.Open();
            string sql = @"DELETE FROM 'group' WHERE group_index=$index";
            SQLiteCommand sqLiteCommand = new SQLiteCommand();
            sqLiteCommand.Connection = sqliteLink.GetConnection();
            sqLiteCommand.CommandText = sql;
            sqLiteCommand.Parameters.Add("$index", DbType.String).Value = groupModel.GroupIndex;
            try
            {
                sqLiteCommand.ExecuteNonQuery();
            }
            catch (Exception)
            {
                sqliteLink.Close();
                throw;
            }

            sqliteLink.Close();
        }
    }

    internal class EventModel
    {
        public EventModel(string eventIndex, string eventBrief, DateTime eventStartDate, string eventRemark=null, string eventCompletion="0", string eventEndDate=null)
        {
            EventIndex = eventIndex;
            EventBrief = eventBrief;
            EventStartDate = eventStartDate;
            EventRemark = eventRemark;
            EventCompletion = eventCompletion;
            EventEndDate = eventEndDate;
        }

        public string EventIndex { get; set; }
        public string EventBrief { get; set; }
        public DateTime EventStartDate { get; set; }
        public string EventRemark { get; set; }
        public string EventCompletion { get; set; }
        public string EventEndDate { get; set; }
    }

    internal static class EventDao
    {
        public static void InsertNewEvent(SqliteLink sqliteLink, EventModel eventModel)
        {
            sqliteLink.Open();
            string sql =
                @"INSERT INTO 'event'(event_index, event_brief, event_start_date, event_remark, event_completion, event_end_date) VALUES ($index, $brief, $date, $remark, $comp, $end)";
            SQLiteCommand sqLiteCommand = new SQLiteCommand();
            sqLiteCommand.Connection = sqliteLink.GetConnection();
            sqLiteCommand.CommandText = sql;
            sqLiteCommand.Parameters.Add("$index", DbType.String).Value = eventModel.EventIndex;
            sqLiteCommand.Parameters.Add("$brief", DbType.String).Value = eventModel.EventBrief;
            sqLiteCommand.Parameters.Add("$date", DbType.String).Value =
                eventModel.EventStartDate.ToString("yyyy-MM-dd");
            sqLiteCommand.Parameters.Add("remark", DbType.String).Value = eventModel.EventRemark;
            sqLiteCommand.Parameters.Add("$comp", DbType.Int64).Value = eventModel.EventCompletion;
            sqLiteCommand.Parameters.Add("$end", DbType.String).Value = eventModel.EventEndDate;
            try
            {
                sqLiteCommand.ExecuteNonQuery();
            }
            catch (Exception)
            {
                sqliteLink.Close();
                throw;
            }

            sqliteLink.Close();
        }

        public static List<EventModel> QueryEventModelsByGroupIndex(SqliteLink sqliteLink, GroupModel groupModel)
        {
            sqliteLink.Open();
            string sql =
                @"SELECT * FROM 'event' WHERE event_index in (SELECT event_index FROM 'group_event' WHERE group_index=$index) ORDER BY event_completion;";
            SQLiteCommand sqLiteCommand = new SQLiteCommand();
            sqLiteCommand.Connection = sqliteLink.GetConnection();
            sqLiteCommand.CommandText = sql;
            sqLiteCommand.Parameters.Add("$index", DbType.String).Value = groupModel.GroupIndex;

            List<EventModel> eventModels = new List<EventModel>();
            using (var reader = sqLiteCommand.ExecuteReader())
            {
                DateTimeFormatInfo format = new DateTimeFormatInfo
                {
                    ShortDatePattern = "yyyy-MM-dd"
                };
                while (reader.Read())
                {
                    // var eventRemark = () =>
                    // {
                    //     if (reader["event_remark"].GetType() == typeof(DBNull)) return null;
                    //     else return (string)reader["event_remark"];
                    // }
                    eventModels.Add(new EventModel((string)reader["event_index"], (string)reader["event_brief"],
                        Convert.ToDateTime((string)reader["event_start_date"], format),
                        reader["event_remark"] is DBNull ? null : (string)reader["event_remark"], 
                        (string)reader["event_completion"], reader["event_end_date"] is DBNull ? null : (string)reader["event_end_date"]));
                }
            }

            sqliteLink.Close();
            return eventModels;
        }

        public static void DeleteEventModelByEventIndex(SqliteLink sqliteLink, string eventIndex)
        {
            sqliteLink.Open();
            string sql = @"DELETE FROM 'event' WHERE event_index=$index";
            SQLiteCommand sqLiteCommand = new SQLiteCommand();
            sqLiteCommand.Connection = sqliteLink.GetConnection();
            sqLiteCommand.CommandText = sql;
            sqLiteCommand.Parameters.Add("$index", DbType.String).Value = eventIndex;
            sqLiteCommand.ExecuteNonQuery();
            sqliteLink.Close();
        }

        public static void UpdateEventModel(SqliteLink sqliteLink, EventModel eventModel)
        {
            sqliteLink.Open();
            string sql =
                @"UPDATE 'event' SET event_brief=$brief, event_start_date=$date, event_remark=$remark, event_completion=$comp, event_end_date=$end WHERE event_index=$index;";
            SQLiteCommand sqLiteCommand = new SQLiteCommand();
            sqLiteCommand.Connection = sqliteLink.GetConnection();
            sqLiteCommand.CommandText = sql;
            sqLiteCommand.Parameters.Add("$brief", DbType.String).Value = eventModel.EventBrief;
            sqLiteCommand.Parameters.Add("$date", DbType.String).Value =
                eventModel.EventStartDate.ToString("yyyy-MM-dd");
            sqLiteCommand.Parameters.Add("$remark", DbType.String).Value = eventModel.EventRemark;
            sqLiteCommand.Parameters.Add("$comp", DbType.Int64).Value = eventModel.EventCompletion;
            sqLiteCommand.Parameters.Add("$index", DbType.String).Value = eventModel.EventIndex;
            sqLiteCommand.Parameters.Add("$end", DbType.String).Value = eventModel.EventEndDate;
            sqLiteCommand.ExecuteNonQuery();
            sqliteLink.Close();
        }

        public static EventModel QueryEventModelByEventIndex(SqliteLink sqliteLink, string eventIndex)
        {
            sqliteLink.Open();
            string sql = @"SELECT * FROM 'event' WHERE event_index=$index";
            SQLiteCommand sqLiteCommand = new SQLiteCommand();
            sqLiteCommand.Connection = sqliteLink.GetConnection();
            sqLiteCommand.CommandText = sql;
            sqLiteCommand.Parameters.Add("$index", DbType.String).Value = eventIndex;

            using (var reader = sqLiteCommand.ExecuteReader())
            {
                DateTimeFormatInfo format = new DateTimeFormatInfo()
                {
                    ShortDatePattern = "yyyy-MM-dd"
                };
                if (reader.Read())
                    return new EventModel((string)reader["event_index"], (string)reader["event_brief"],
                        Convert.ToDateTime((string)reader["event_start_date"], format),
                        reader["event_remark"] is DBNull ? null : (string)reader["event_remark"],
                        (string)reader["event_completion"], reader["event_end_date"] is DBNull ? null : (string)reader["event_end_date"]);
            }

            throw new Exception();
        }
    }

    internal class GroupEventModel
    {
            public GroupEventModel(GroupModel groupModel, EventModel eventModel)
            {
                GroupModel = groupModel;
                EventModel = eventModel;
            }

            public GroupModel GroupModel { get; set; }
            public EventModel EventModel { get; set; }
    }

    internal static class GroupEventDao 
    {
        public static void InsertNewGroupEventRelation(SqliteLink sqliteLink, GroupEventModel groupEventModel)
        {
            sqliteLink.Open();
            string sql = @"INSERT INTO 'group_event'(group_index, event_index) VALUES ($group, $event)";
            SQLiteCommand sqLiteCommand = new SQLiteCommand();
            sqLiteCommand.Connection = sqliteLink.GetConnection();
            sqLiteCommand.CommandText = sql;
            sqLiteCommand.Parameters.Add("$group", DbType.String).Value = groupEventModel.GroupModel.GroupIndex;
            sqLiteCommand.Parameters.Add("$event", DbType.String).Value = groupEventModel.EventModel.EventIndex;
            try
            {
                sqLiteCommand.ExecuteNonQuery();
            }
            catch (Exception)
            {
                sqliteLink.Close();
                throw;
            }
            sqliteLink.Close();
        }

        public static void DeleteAllGroupEventRelationsByGroupIndex(SqliteLink sqliteLink, GroupModel groupModel)
        {
            string sql = @"DELETE FROM 'group_event' WHERE group_index=$index";
            SQLiteCommand sqLiteCommand = new SQLiteCommand();
            sqLiteCommand.Connection = sqliteLink.GetConnection();
            sqLiteCommand.CommandText = sql;
            sqLiteCommand.Parameters.Add("$index", DbType.String).Value = groupModel.GroupIndex;
            sqLiteCommand.ExecuteNonQuery(); 
            sqliteLink.Close();
        }

        public static List<string> QueryAllEventIndexesByGroupIndex(SqliteLink sqliteLink, GroupModel groupModel)
        {
            sqliteLink.Open();
            string sql = @"SELECT event_index FROM 'group_event' WHERE group_index=$index";
            SQLiteCommand sqLiteCommand = new SQLiteCommand();
            sqLiteCommand.Connection = sqliteLink.GetConnection();
            sqLiteCommand.CommandText = sql;
            sqLiteCommand.Parameters.Add("$index", DbType.String).Value = groupModel.GroupIndex;
            List<string> eventIndexes = new List<string>();
            using (var reader = sqLiteCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    eventIndexes.Add((string)reader["event_index"]);
                }
            }
            
            sqliteLink.Close();
            return eventIndexes;
        }

        public static List<string> CheckEventIndexesExistsByEventIndex(SqliteLink sqliteLink, List<string> eventIndexes)
        {
            sqliteLink.Open();
            string sql = @"SELECT * FROM 'group_event' WHERE event_index=$index";
            SQLiteCommand sqLiteCommand = new SQLiteCommand();
            sqLiteCommand.Connection = sqliteLink.GetConnection();
            List<string> deleteEventIndexes = new List<string>();
            foreach (var eventIndex in eventIndexes)
            {
                sqLiteCommand.CommandText = sql;
                sqLiteCommand.Parameters.Add("$index", DbType.String).Value = eventIndex;
                using (var reader = sqLiteCommand.ExecuteReader())
                {
                    if (!reader.Read()) deleteEventIndexes.Add(eventIndex);
                }
            }
            sqliteLink.Close();

            return deleteEventIndexes;
        }

        public static void DeleteGroupEventRelationByGroupIndexAndEventIndex(SqliteLink sqliteLink, string groupIndex,
            string eventIndex)
        {
            sqliteLink.Open();
            string sql = @"DELETE FROM 'group_event' WHERE group_index=$group AND event_index=$event";
            SQLiteCommand sqLiteCommand = new SQLiteCommand();
            sqLiteCommand.Connection = sqliteLink.GetConnection();
            sqLiteCommand.CommandText = sql;
            sqLiteCommand.Parameters.Add("$group", DbType.String).Value = groupIndex;
            sqLiteCommand.Parameters.Add("$event", DbType.String).Value = eventIndex;
            sqLiteCommand.ExecuteNonQuery();
            sqliteLink.Close();
        }
    }
}
