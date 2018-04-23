Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Reflection
Imports System.Windows.Forms
Imports System.Linq.Expressions
Imports DevExpress.XtraScheduler

Namespace SchedulerControlEventViewer
    Public Class SchedulerEventHooker
        #Region "Fields"
        Private eventName As String
        Private owner As Form1
        Private handler As System.Delegate
        #End Region

        Public Sub New(ByVal eventName As String, ByVal control As Object, ByVal owner As Form1)
            Me.eventName = eventName
            Me.owner = owner
            Me.Control = control
        End Sub

        #Region "Properties"
        Public ReadOnly Property Name() As String
            Get
                Return eventName
            End Get
        End Property
        Protected Overridable ReadOnly Property MethodName() As String
            Get
                Return "InitializeEventLogger"
            End Get
        End Property
        Public Control As Object
        #End Region

        #Region "CreateEventProxy"
        Private Function CreateEventProxy(ByVal eInfo As EventInfo) As System.Delegate
            Dim eventHandlerType As Type = eInfo.EventHandlerType
            Dim mInfo As MethodInfo = eventHandlerType.GetMethod("Invoke")
            Dim loggerMethodInfo As MethodInfo = GetType(Form1).GetMethod(MethodName, BindingFlags.NonPublic Or BindingFlags.Public Or BindingFlags.Instance Or BindingFlags.Static)
            Dim pInfo() As ParameterInfo = mInfo.GetParameters()
            Dim eventSender As ParameterExpression = Expression.Parameter(GetType(Object), "sender")
            Dim eventArgs As ParameterExpression = Expression.Parameter(pInfo(1).ParameterType, "args")
            Return Expression.Lambda(eventHandlerType, Expression.Call(Expression.Constant(owner), loggerMethodInfo, Expression.Constant(eventName), eventSender, eventArgs), eventSender, eventArgs).Compile()
        End Function
        #End Region

        Public Function HookEvent() As Boolean
            Dim controlType As Type = Control.GetType()
            Dim eInfo As EventInfo = controlType.GetEvent(eventName)
            If handler Is Nothing Then
                handler = CreateEventProxy(eInfo)
            End If
            Try
                eInfo.AddEventHandler(Control, handler)
            Catch e As ArgumentException
                MessageBox.Show(e.Message, "Cannot hook an event", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return False
            End Try
            Return True
        End Function

        Public Sub UnhookEvent()
            If handler IsNot Nothing Then
                Dim controlType As Type = Control.GetType()
                Dim eInfo As EventInfo = controlType.GetEvent(eventName)
                eInfo.RemoveEventHandler(Control, handler)
            End If
        End Sub
    End Class
End Namespace
