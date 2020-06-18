using Prism.Events;
using System;

namespace UnitPlanGenerator.Events
{
    public enum UserCollectionChangedAction
    {
        Add,
        Remove,
        Replace,
    }

    public class UserCollectionChangedEventArgs : EventArgs
    {
        public int UserId { get; set; }

        public UserCollectionChangedAction Action { get; set; }

        public UserCollectionChangedEventArgs(int userId, UserCollectionChangedAction action)
        {
            UserId = userId;
            Action = action;
        }
    }

    public class UserCollectionChangedEvent : PubSubEvent<UserCollectionChangedEventArgs> { }
}
