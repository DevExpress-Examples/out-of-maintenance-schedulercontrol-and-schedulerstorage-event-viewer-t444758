using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Linq.Expressions;
using DevExpress.XtraScheduler;

namespace SchedulerControlEventViewer
    {
    public class SchedulerEventHooker
    {
        #region Fields
        string eventName;
        Form1 owner;
        Delegate handler;
        #endregion

        public SchedulerEventHooker(string eventName, object control, Form1 owner)
        {
            this.eventName = eventName;
            this.owner = owner;
            this.Control = control;
        }

        #region Properties
        public string Name { get { return eventName; } }
        protected virtual string MethodName { get { return "InitializeEventLogger"; } }
        public object Control;
        #endregion

        #region CreateEventProxy
        Delegate CreateEventProxy(EventInfo eInfo)
        {
            Type eventHandlerType = eInfo.EventHandlerType;
            MethodInfo mInfo = eventHandlerType.GetMethod("Invoke");
            MethodInfo loggerMethodInfo = typeof(Form1).GetMethod(MethodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            ParameterInfo[] pInfo = mInfo.GetParameters();
            ParameterExpression eventSender = Expression.Parameter(typeof(object), "sender");
            ParameterExpression eventArgs = Expression.Parameter(pInfo[1].ParameterType, "args");
            return Expression.Lambda(eventHandlerType, Expression.Call(Expression.Constant(owner), loggerMethodInfo, Expression.Constant(eventName), eventSender, eventArgs), eventSender, eventArgs).Compile();
        }
        #endregion

        public bool HookEvent()
        {
            Type controlType = Control.GetType();
            EventInfo eInfo = controlType.GetEvent(eventName);
            if (handler == null)
                handler = CreateEventProxy(eInfo);
            try
            {
                eInfo.AddEventHandler(Control, handler);
            }
            catch (ArgumentException e)
            {
                MessageBox.Show(e.Message, "Cannot hook an event", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }

        public void UnhookEvent()
        {
            if (handler != null)
            {
                Type controlType = Control.GetType();
                EventInfo eInfo = controlType.GetEvent(eventName);
                eInfo.RemoveEventHandler(Control, handler);
            }
        }
    }
}
