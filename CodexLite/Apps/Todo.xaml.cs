using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Animation;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace CodexLite.Apps
{
    /// <summary>
    /// Todo.xaml 的交互逻辑
    /// </summary>
    public partial class Todo
    {
        /// <summary>
        /// 重新包装的sqliteConnection，方便每次调用。
        /// </summary>
        private static SqliteLink _sqliteLink;
        /// <summary>
        /// 事件组资源绑定
        /// </summary>
        private static BindingSource _groupModelBindingSource;
        /// <summary>
        /// 每个组中的全部事件资源绑定
        /// </summary>
        private static BindingSource _eventModelBindingSource;

        public Todo()
        {
            InitializeComponent();
            _sqliteLink = new SqliteLink();     // 初始化一个预包装的sqlite链接
            _groupModelBindingSource = new BindingSource();
            _eventModelBindingSource = new BindingSource();

            // 查询全部分组，将ListBox和BindingSource绑定，同步更新UI。
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
                // Pass
            }
        }

        /// <summary>
        /// UpperListBox的选择处理函数，选择改变事件
        /// <para>由于页面设计原因，将ListBox分成了两个，分为UpperListBox和GroupListBox,但是两个在逻辑上应该共用一个SelectedItem，需要进行手动逻辑处理。</para>
        /// <para>在选择完成后需要显示选择的Group的全部Event，同时显示顶部的提示控件和添加事件的控件</para>
        /// </summary>
        private void UpperListBoxSelected_OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            // 双ListBOX选择项处理。
            if (GroupListBox.SelectedItem != null && UpperListBox.SelectedItem != null)
            {
                GroupListBox.SelectedItem = null;
            }

            // 选择改变时的逻辑处理，如果选择了一个Group，则做对应查询和数据绑定处理，否则将相关UI数据资源重置为null阻止其显示在页面.
            ListBoxItem selectedItem = (ListBoxItem)UpperListBox.SelectedItem;
            if (selectedItem != null && (string)selectedItem.DataContext == "0")
            {
                EventToolbarContentControl.ContentTemplate = Resources["ImportantToolbarTemplate"] as DataTemplate;     // 提示控件的DataTemplate选择与绑定。
                NewEventBorder.Visibility = Visibility.Visible;     // 显示添加事件的控件
                NewEventButton.DataContext = new GroupModel("0", "重要", 0);
                _eventModelBindingSource.DataSource =
                    EventDao.QueryEventModelsByGroupIndex(_sqliteLink, NewEventButton.DataContext as GroupModel);   // 查询分组的事件
                EventListBox.ItemsSource = _eventModelBindingSource;    // 事件资源绑定
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

        /// <summary>
        /// GroupListBox的选择处理函数，选择改变事件
        /// <seealso cref="UpperListBoxSelected_OnSelectionChanged"/>
        /// </summary>
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

        /// <summary>
        /// EventListBox的选择处理函数，选择改变事件
        /// </summary>
        private void EventListBoxSelected_OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            if (EventListBox.SelectedItem != null) // 如果选择非空，那么将事件显示区域的上下文绑定为选择的事件，开始伸展的动画
            {
                var rightBorderAnimation = Resources["RightBorderExtendStoryboard"] as Storyboard;
                rightBorderAnimation?.Begin();
                RightGrid.DataContext = EventListBox.SelectedItem;

            }
            else    // 否则将事件显示区域上下文重置为null，开始缩回的动画
            {
                var rightBorderAnimation = Resources["RightBorderNarrowStoryboard"] as Storyboard;
                rightBorderAnimation?.Begin();
                RightGrid.DataContext = null;
            }
        }

        /// <summary>
        /// 添加一个新Group，按钮点击事件
        /// </summary>
        private void InsertNewGroup_OnClick(object sender, RoutedEventArgs e)
        {
            if (NewGroupTextBox.Text == String.Empty) return; // 如果Group名称为空则返回不做处理


            string timeStamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds().ToString(); // 利用时间戳生成编号
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

        /// <summary>
        /// 添加一个新Group，案件响应事件
        /// </summary>
        private void InsertNewGroup_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (NewGroupTextBox.Text == String.Empty || e.Key != Key.Enter) return;     // Group名称不为空且按下Enter才会处理

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

        /// <summary>
        /// 删除Group，按钮点击事件
        /// <para>删除并不是简单的删除将数据库中的Group删除就好了，由于SQLite缺乏强制外键约束，有必要对引用完整性做检查，具体的删除步骤是: 1.首先检查Group组中的所有的Event，是否存在其他的关系约束，如果不存在，则把这些Events也删除。2.删除这个Group。</para>
        /// </summary>
        private void DeleteGroup_OnClick(object sender, RoutedEventArgs e)
        {
            var button = (System.Windows.Controls.Button)sender;
            List<string> eventIndexes =
                GroupEventDao.QueryAllEventIndexesByGroupIndex(_sqliteLink, button.DataContext as GroupModel);  // 查询Group中的全部事件。
            GroupEventDao.DeleteAllGroupEventRelationsByGroupIndex(_sqliteLink, button.DataContext as GroupModel);  // 将要删除的Group和这些事件的关系删除。
            List<string> deleteEventIndexes =
                GroupEventDao.CheckEventIndexesExistsByEventIndex(_sqliteLink, eventIndexes);   // 查询Event是否还存在其他外键约束，如果不存在则准备删除。
            foreach (var deleteEventIndex in deleteEventIndexes)
            {
                EventDao.DeleteEventModelByEventIndex(_sqliteLink, deleteEventIndex);   // 删除不存在额外约束的Event。
            }
            GroupDao.DeleteGroup(_sqliteLink, button.DataContext as GroupModel);    // 删除这个组。
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

        /// <summary>
        /// 添加一个新Event，按钮点击事件
        /// </summary>
        private void InsertNewEvent_OnClick(object sender, RoutedEventArgs e)
        {
            if (NewEventTextBox.Text == String.Empty) return;
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
                    GroupEventDao.InsertNewGroupEventRelation(_sqliteLink, newGroupEventModel);     // 添加Group和Event的关系
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

        /// <summary>
        /// 添加一个新Event，选择改变事件
        /// </summary>
        private void InsertNewEvent_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (NewEventTextBox.Text == String.Empty || e.Key != Key.Enter) return;
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

        /// <summary>
        /// 改变Event的完成状态，按钮点击事件
        /// </summary>
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

        /// <summary>
        /// 更新Event，按钮点击事件
        /// </summary>
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

        /// <summary>
        /// 放弃Event的改变，按钮点击事件
        /// <para>重新做一次查询获取Event的信息</para>
        /// </summary>
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

        /// <summary>
        /// Event展示面板收缩，按钮点击事件
        /// </summary>
        private void NarrowRightBorder_OnClick(object sender, RoutedEventArgs e)
        {
            var rightBorderAnimation = Resources["RightBorderNarrowStoryboard"] as Storyboard;
            rightBorderAnimation?.Begin();
            EventListBox.SelectedItem = null;
        }

        
        /// <summary>
        /// Event删除，按钮点击事件
        /// <para>同样为了安全删除，需要手动检查外键约束。1.先删除对应的Group和Event的关系。2.检查有无剩余Event外键约束，如果没有，则删除Event。</para>
        /// </summary>
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

    /// <summary>
    /// Converter，转化事件的创建日期，从DateTime格式化为字符串。
    /// <para>注意，事件的创建日期是隐式只读的，所以尝试反向转化会抛出异常</para>
    /// <para>如果创建日期为今天或者昨天，则显示对应汉字，否则以yyyy-MM-dd的形式展示日期</para>
    /// <para>尝试转化null会返回空字符串String.Empty</para>
    /// </summary>
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

    /// <summary>
    /// Converter，转化事件的结束日期，从DateTime格式化为字符串。
    /// <para>在结束日期的展示和选择控件中，DatePicker和TextBox是分离的，这意味着展示的文本框也是隐式只读的，尝试反向转化会抛出异常，如果要获取结束日期，应该从DatePicker获取。</para>
    /// <para>如果结束时间为昨天，今天或者明天，会显示汉字，否则以yyyy-MM-dd的形式展示日期</para>
    /// <para>尝试转化null会返回空字符串String.Empty</para>
    /// </summary>
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

    /// <summary>
    /// Converter，转化事件的结束日期，将DateTime与字符串互相转化。
    /// <para>这个转化器是针对DatePicker的，DatePicker的选择日期是DateTime对象，而SQLite并不支持Date类型的存储格式，需要将日期格式化字符串和DateTime互相转化。</para>
    /// <para>格式化字符串应严格遵循yyyy-MM-dd的格式，任何不同格式都将导致抛出异常</para>
    /// <para>无论正向还是反向，尝试转化null时均会返回null</para>
    /// </summary>
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

    /// <summary>
    /// 重新封装的sqliteConnection，将链接字符串固定
    /// </summary>
    internal class SqliteLink
    {
        private readonly SQLiteConnection _sqliteConnection;
        public SQLiteConnection SqLiteConnection => _sqliteConnection;

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
    }

    /// <summary>
    /// Group类，数据库Group表的实体模型。
    /// </summary>
    internal class GroupModel
    {
        public GroupModel(string groupIndex, string groupName, long editable = 1)
        {
            GroupIndex = groupIndex;
            GroupName = groupName;
            Editable = editable;
        }

        /// <summary>
        /// 分组索引，主键，由时间戳自动生成
        /// </summary>
        public string GroupIndex { get; set; }
        /// <summary>
        /// 分组名称
        /// </summary>
        public string GroupName { get; set; }
        /// <summary>
        /// 分组是否可以被编辑，默认为1表示可以编辑
        /// </summary>
        public long Editable { get; set; }
    }

    /// <summary>
    /// Group表的DAO层，用于Group数据模型和数据库的交互。
    /// </summary>
    internal static class GroupDao
    {
        /// <summary>
        /// 查询全部的Group
        /// </summary>
        /// <param name="sqliteLink">实例化好的sqliteLink</param>
        /// <returns>含有全部Group信息的Group实体的列表</returns>
        public static List<GroupModel> QueryAllGroupModels(SqliteLink sqliteLink)
        {
            sqliteLink.Open();
            string sql = "SELECT * FROM 'group' WHERE editable=1";
            List<GroupModel> groupIndexModels = new List<GroupModel>();
            SQLiteCommand sqliteCommand = new SQLiteCommand(sql, sqliteLink.SqLiteConnection);
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

        /// <summary>
        /// 插入Group
        /// </summary>
        /// <param name="sqliteLink">实例化好的sqliteLink</param>
        /// <param name="groupModel">待插入的Group实体</param>
        public static void InsertNewGroup(SqliteLink sqliteLink, GroupModel groupModel)
        {
            sqliteLink.Open();
            string sql =
                @"INSERT INTO 'group'(group_index, group_name, editable) VALUES ($index, $name, $editable)";
            SQLiteCommand sqLiteCommand = new SQLiteCommand();
            sqLiteCommand.Connection = sqliteLink.SqLiteConnection;
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

        /// <summary>
        /// 删除Group
        /// </summary>
        /// <param name="sqliteLink">实例化好的sqliteLink</param>
        /// <param name="groupModel">待删除的Group实体，根据group_index来确定删除的实体。</param>
        public static void DeleteGroup(SqliteLink sqliteLink, GroupModel groupModel)
        {
            sqliteLink.Open();
            string sql = @"DELETE FROM 'group' WHERE group_index=$index";
            SQLiteCommand sqLiteCommand = new SQLiteCommand();
            sqLiteCommand.Connection = sqliteLink.SqLiteConnection;
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

    /// <summary>
    /// Event类，Event表的实体映射模型。
    /// </summary>
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

        /// <summary>
        /// 事件索引，由时间戳生成。not nullable.
        /// </summary>
        public string EventIndex { get; set; }
        /// <summary>
        /// 事件简述，用来作为首要展示的内容。not nullable.
        /// </summary>
        public string EventBrief { get; set; }
        /// <summary>
        /// 事件创建日期, not nullable，yyyy-MM-dd.
        /// </summary>
        public DateTime EventStartDate { get; set; }
        /// <summary>
        /// 事件的备注，默认为null。
        /// </summary>
        public string EventRemark { get; set; }
        /// <summary>
        /// 事件完成状态，默认为0，表示未完成，完成则为1.
        /// </summary>
        public string EventCompletion { get; set; }
        /// <summary>
        /// 设置的事件的结束日期，默认为null。
        /// </summary>
        public string EventEndDate { get; set; }
    }

    /// <summary>
    /// Event表的DAO层，用于Event实体和数据库的交互。
    /// </summary>
    internal static class EventDao
    {
        /// <summary>
        /// 插入Event。
        /// </summary>
        /// <param name="sqliteLink">实例化好的sqliteLink。</param>
        /// <param name="eventModel">待插入的Event实体。</param>
        public static void InsertNewEvent(SqliteLink sqliteLink, EventModel eventModel)
        {
            sqliteLink.Open();
            string sql =
                @"INSERT INTO 'event'(event_index, event_brief, event_start_date, event_remark, event_completion, event_end_date) VALUES ($index, $brief, $date, $remark, $comp, $end)";
            SQLiteCommand sqLiteCommand = new SQLiteCommand();
            sqLiteCommand.Connection = sqliteLink.SqLiteConnection;
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

        /// <summary>
        /// 根据GroupIndex查询联系后获取全部有联系的Event。这不是一个原子查询。
        /// </summary>
        /// <param name="sqliteLink">实例化好的sqliteLink。</param>
        /// <param name="groupModel">待查询的Group实体</param>
        /// <returns>与Group实体在group_event表中有约束关系的Event实体列表，根据完成状态进行了排序，未完成的将排在前面。</returns>
        public static List<EventModel> QueryEventModelsByGroupIndex(SqliteLink sqliteLink, GroupModel groupModel)
        {
            sqliteLink.Open();
            string sql =
                @"SELECT * FROM 'event' WHERE event_index in (SELECT event_index FROM 'group_event' WHERE group_index=$index) ORDER BY event_completion;";
            SQLiteCommand sqLiteCommand = new SQLiteCommand();
            sqLiteCommand.Connection = sqliteLink.SqLiteConnection;
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

        /// <summary>
        /// 根据EventIndex删除对应的Event实体。
        /// </summary>
        /// <param name="sqliteLink">实例化好的sqliteLink。</param>
        /// <param name="eventIndex">待删除的Event实体的EventIndex。</param>
        public static void DeleteEventModelByEventIndex(SqliteLink sqliteLink, string eventIndex)
        {
            sqliteLink.Open();
            string sql = @"DELETE FROM 'event' WHERE event_index=$index";
            SQLiteCommand sqLiteCommand = new SQLiteCommand();
            sqLiteCommand.Connection = sqliteLink.SqLiteConnection;
            sqLiteCommand.CommandText = sql;
            sqLiteCommand.Parameters.Add("$index", DbType.String).Value = eventIndex;
            sqLiteCommand.ExecuteNonQuery();
            sqliteLink.Close();
        }

        /// <summary>
        /// 更新Event实体。
        /// </summary>
        /// <param name="sqliteLink">实例化好的sqliteLink。</param>
        /// <param name="eventModel">待更新的Event实体。</param>
        public static void UpdateEventModel(SqliteLink sqliteLink, EventModel eventModel)
        {
            sqliteLink.Open();
            string sql =
                @"UPDATE 'event' SET event_brief=$brief, event_start_date=$date, event_remark=$remark, event_completion=$comp, event_end_date=$end WHERE event_index=$index;";
            SQLiteCommand sqLiteCommand = new SQLiteCommand();
            sqLiteCommand.Connection = sqliteLink.SqLiteConnection;
            sqLiteCommand.CommandText = sql;
            sqLiteCommand.Parameters.Add("$brief", DbType.String).Value = eventModel.EventBrief;
            sqLiteCommand.Parameters.Add("$date", DbType.String).Value =
                eventModel.EventStartDate.ToString("yyyy-MM-dd");
            sqLiteCommand.Parameters.Add("$remark", DbType.String).Value = eventModel.EventRemark;
            sqLiteCommand.Parameters.Add("$comp", DbType.String).Value = eventModel.EventCompletion;
            sqLiteCommand.Parameters.Add("$index", DbType.String).Value = eventModel.EventIndex;
            sqLiteCommand.Parameters.Add("$end", DbType.String).Value = eventModel.EventEndDate;
            sqLiteCommand.ExecuteNonQuery();
            sqliteLink.Close();
        }

        /// <summary>
        /// 根据EventIndex查询Event实体。
        /// </summary>
        /// <param name="sqliteLink">实例化好的sqliteLink。</param>
        /// <param name="eventIndex">待查询的Event实体的EventIndex</param>
        /// <returns>查询的Event实体</returns>
        /// <exception cref="Exception">如果查询失败则会抛出异常</exception>
        public static EventModel QueryEventModelByEventIndex(SqliteLink sqliteLink, string eventIndex)
        {
            sqliteLink.Open();
            string sql = @"SELECT * FROM 'event' WHERE event_index=$index";
            SQLiteCommand sqLiteCommand = new SQLiteCommand();
            sqLiteCommand.Connection = sqliteLink.SqLiteConnection;
            sqLiteCommand.CommandText = sql;
            sqLiteCommand.Parameters.Add("$index", DbType.String).Value = eventIndex;

            using var reader = sqLiteCommand.ExecuteReader();
            DateTimeFormatInfo format = new DateTimeFormatInfo()
            {
                ShortDatePattern = "yyyy-MM-dd"
            };
            if (reader.Read())
                return new EventModel((string)reader["event_index"], (string)reader["event_brief"],
                    Convert.ToDateTime((string)reader["event_start_date"], format),
                    reader["event_remark"] is DBNull ? null : (string)reader["event_remark"],
                    (string)reader["event_completion"], reader["event_end_date"] is DBNull ? null : (string)reader["event_end_date"]);

            throw new Exception();
        }
    }

    /// <summary>
    /// GroupEvent类，GroupEvent表的约束弱实体，将Group与Event映射为多对多的关系。
    /// </summary>
    internal class GroupEventModel
    {
            public GroupEventModel(GroupModel groupModel, EventModel eventModel)
            {
                GroupModel = groupModel;
                EventModel = eventModel;
            }

            /// <summary>
            /// 映射关系中的Group实体。
            /// </summary>
            public GroupModel GroupModel { get; set; }
            /// <summary>
            /// 映射关系中的Event实体。
            /// </summary>
            public EventModel EventModel { get; set; }
    }

    /// <summary>
    /// GroupEvent表的DAO层，用于GroupEvent弱实体和数据库的交互。
    /// </summary>
    internal static class GroupEventDao 
    {
        /// <summary>
        /// 插入新的GroupEvent约束实体。
        /// </summary>
        /// <param name="sqliteLink">实例化好的sqliteLink。</param>
        /// <param name="groupEventModel">待插入的GroupEvent实体。</param>
        public static void InsertNewGroupEventRelation(SqliteLink sqliteLink, GroupEventModel groupEventModel)
        {
            sqliteLink.Open();
            string sql = @"INSERT INTO 'group_event'(group_index, event_index) VALUES ($group, $event)";
            SQLiteCommand sqLiteCommand = new SQLiteCommand();
            sqLiteCommand.Connection = sqliteLink.SqLiteConnection;
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

        /// <summary>
        /// 根据GroupIndex删除所有对应的GroupEvent约束实体。
        /// </summary>
        /// <param name="sqliteLink">实例化好的sqliteLink。</param>
        /// <param name="groupModel">待查找的GroupIndex的Group实体。</param>
        public static void DeleteAllGroupEventRelationsByGroupIndex(SqliteLink sqliteLink, GroupModel groupModel)
        {
            string sql = @"DELETE FROM 'group_event' WHERE group_index=$index";
            SQLiteCommand sqLiteCommand = new SQLiteCommand();
            sqLiteCommand.Connection = sqliteLink.SqLiteConnection;
            sqLiteCommand.CommandText = sql;
            sqLiteCommand.Parameters.Add("$index", DbType.String).Value = groupModel.GroupIndex;
            sqLiteCommand.ExecuteNonQuery(); 
            sqliteLink.Close();
        }

        /// <summary>
        /// 根据GroupIndex查找所有的GroupEvent约束实体。
        /// </summary>
        /// <param name="sqliteLink">实例化好的sqliteLink。</param>
        /// <param name="groupModel">待查找的GroupIndex的Group实体。</param>
        /// <returns></returns>
        public static List<string> QueryAllEventIndexesByGroupIndex(SqliteLink sqliteLink, GroupModel groupModel)
        {
            sqliteLink.Open();
            string sql = @"SELECT event_index FROM 'group_event' WHERE group_index=$index";
            SQLiteCommand sqLiteCommand = new SQLiteCommand();
            sqLiteCommand.Connection = sqliteLink.SqLiteConnection;
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

        /// <summary>
        /// 根据EventIndex集合的列表查询是否存在对应的GroupEvent约束实体。
        /// </summary>
        /// <param name="sqliteLink">实例化好的sqliteLink。</param>
        /// <param name="eventIndexes">待查询的EventIndex集合的列表</param>
        /// <returns>如果不存在对应GroupEvent约束实体的EventIndex集合的列表</returns>
        public static List<string> CheckEventIndexesExistsByEventIndex(SqliteLink sqliteLink, List<string> eventIndexes)
        {
            sqliteLink.Open();
            string sql = @"SELECT * FROM 'group_event' WHERE event_index=$index";
            SQLiteCommand sqLiteCommand = new SQLiteCommand();
            sqLiteCommand.Connection = sqliteLink.SqLiteConnection;
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

        /// <summary>
        /// 根据GroupIndex和EventIndex删除对应的GroupEvent约束实体。
        /// </summary>
        /// <param name="sqliteLink">实例化好的sqliteLink。</param>
        /// <param name="groupIndex">待删除的GroupEvent约束实体的GroupIndex</param>
        /// <param name="eventIndex">待删除的GroupEvent约束实体的EventIndex</param>
        public static void DeleteGroupEventRelationByGroupIndexAndEventIndex(SqliteLink sqliteLink, string groupIndex,
            string eventIndex)
        {
            sqliteLink.Open();
            string sql = @"DELETE FROM 'group_event' WHERE group_index=$group AND event_index=$event";
            SQLiteCommand sqLiteCommand = new SQLiteCommand();
            sqLiteCommand.Connection = sqliteLink.SqLiteConnection;
            sqLiteCommand.CommandText = sql;
            sqLiteCommand.Parameters.Add("$group", DbType.String).Value = groupIndex;
            sqLiteCommand.Parameters.Add("$event", DbType.String).Value = eventIndex;
            sqLiteCommand.ExecuteNonQuery();
            sqliteLink.Close();
        }
    }
}
