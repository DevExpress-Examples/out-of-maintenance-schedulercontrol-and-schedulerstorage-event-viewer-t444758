using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraRichEdit;
using DevExpress.XtraRichEdit.API.Native;
using DevExpress.XtraTreeList.Nodes;
using DevExpress.XtraScheduler;

namespace SchedulerControlEventViewer
{
    public partial class Form1 : DevExpress.XtraBars.Ribbon.RibbonForm
    {
        #region Fields
        List<SchedulerEventHooker> hooks;
        List<string> unsafeEvents = new List<string> 
        { "ContextButtonCustomize", "RangeControlAutoAdjusting", "AppointmentViewInfoCustomizing", "InitAppointmentDisplayText", "InitAppointmentImages" };
        #endregion

        public Form1()
        {
            hooks = new List<SchedulerEventHooker>();
            InitializeComponent();
            PrepareContent();
            EnumerateEvents();
            checkAllEdit.CheckState = CheckState.Checked;
            UnCheckFrequentEvents();

        }

        #region static helper functions
        static int SortHooksByName(SchedulerEventHooker arg1, SchedulerEventHooker arg2)
        {
            return arg1.Name.CompareTo(arg2.Name);
        }
        #endregion

        void EnumerateEvents()
        {
            var events1 = HookEvents(schedulerControl1, typeof(SchedulerControl));
            var events2 = HookEvents(schedulerStorage1, typeof(SchedulerStorageBase));
            var events = events1.Concat<EventInfo>(events2);

            hooks.Sort(SortHooksByName);
            for (int i = 0; i < hooks.Count; ++i)
            {
                string eventName = hooks[i].Name;
                eventsCheckedListBox.Items.Add(i, eventName);
            }
        }

        EventInfo[] HookEvents(object control, Type controlType) {
            if (controlType == typeof(SchedulerStorage)) controlType = typeof(SchedulerStorageBase);
            EventInfo[] events = controlType.GetEvents(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            foreach (EventInfo item in events) {
                if (!unsafeEvents.Contains(item.Name)) {
                    string eventName = item.Name;
                    hooks.Add(new SchedulerEventHooker(eventName, control, this));
                }
            }
            return events;
        }


        void PrepareContent()
        {
        }

        string GetEventParameter(PropertyInfo item, object value)
        {
            StringBuilder strBuilder = new StringBuilder();
            strBuilder.Append(item.Name);
            if (item.PropertyType == typeof(string))
            {
                strBuilder.Append("=\'");
                strBuilder.Append(value);
                strBuilder.Append('\'');
            }
            else
            {
                strBuilder.Append('=');
                strBuilder.Append(value);
            }
            return strBuilder.ToString();
        }

        public void InitializeEventLogger(string eventName, object sender, EventArgs args)
        {
            Type argsType = args.GetType();
            string name = eventName;
            string argsTypeName = argsType.Name;
            PropertyInfo[] propInfoArray = argsType.GetProperties();
            TreeListNode rootListNode = null;
            TreeListNode firstLevelNode = loggerControl.AppendNode(new object[] { name, argsTypeName }, rootListNode);
            foreach (PropertyInfo item in propInfoArray)
            {
                string value = GetEventParameter(item, item.GetValue(args, null));
                TreeListNode secondLevelNode = loggerControl.AppendNode(new object[] { "", "", value }, firstLevelNode);
            }
            if (appendExpandedBox.Checked && (propInfoArray.Length > 0))
                firstLevelNode.ExpandAll();
            loggerControl.MakeNodeVisible(firstLevelNode);
        }

        
        #region FormControls
        void eventsCheckedListBox_ItemCheck(object sender, DevExpress.XtraEditors.Controls.ItemCheckEventArgs e)
        {
            int itemIndex = e.Index;
            bool state = e.State == CheckState.Checked;
            int hookIndex = Convert.ToInt32(eventsCheckedListBox.Items[itemIndex].Value);
            if (state) {
                hooks[hookIndex].HookEvent();
                checkAllEdit.CheckState = (eventsCheckedListBox.CheckedItemsCount == eventsCheckedListBox.ItemCount) ? CheckState.Checked : CheckState.Indeterminate;
            }
            else {
                hooks[hookIndex].UnhookEvent();
                checkAllEdit.CheckState = (eventsCheckedListBox.CheckedItemsCount == 0) ? CheckState.Unchecked : CheckState.Indeterminate;
            }
        }

        void checkAllEdit_CheckStateChanged(object sender, EventArgs e)
        {
            CheckState state = checkAllEdit.CheckState;
            switch (state)
            {
                case CheckState.Checked:
                    eventsCheckedListBox.CheckAll();
                    break;
                case CheckState.Unchecked:
                    eventsCheckedListBox.UnCheckAll();
                    break;
            }
        }

        void expandButtonItem_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            loggerControl.ExpandAll();
        }

        void collapseButtonItem_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            loggerControl.CollapseAll();
        }

        void clearButtonItem_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            loggerControl.ClearNodes();
        }

        private void UnCheckFrequentEvents()
        {
            for (int i = 0; i < eventsCheckedListBox.Items.Count; ++i)
            {
                if (eventsCheckedListBox.Items[i].Description.Contains("Draw"))
                    eventsCheckedListBox.Items[i].CheckState = CheckState.Unchecked;
                if (eventsCheckedListBox.Items[i].Description.Contains("Query"))
                    eventsCheckedListBox.Items[i].CheckState = CheckState.Unchecked;
            }
            checkAllEdit.CheckState = CheckState.Indeterminate;
        }
        #endregion FormControls
    }
}
