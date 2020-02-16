using System;
using System.Collections.Generic;

#region 事件参数类型
public class EventBase
{ }

public class EventBool : EventBase
{
    public bool value;
}
public class EventString : EventBase
{
    public string value;
}
public class EventInt : EventBase
{
    public int value;
}
public class EventFloat : EventBase
{
    public float value;
}
#endregion

// 事件管理器
public class GameEventor
{
    private class ReceiptInfo
    {
        public string name_;
        public Action<string, EventBase> delegate_;
        public ReceiptInfo() { }
        public ReceiptInfo(string n, Action<string, EventBase> d)
        {
            name_ = n;
            delegate_ = d;
        }
    }

    public static GameEventor Instance { get { return instance_; } }

    public object Subscribe(string evt, Action<string, EventBase> d)
    {
        Action<string, EventBase> source;
        if (subscribers_.TryGetValue(evt, out source))
        {
            if (null != source)
                if (source.GetType() == d.GetType())
                {
                    source = (Action<string, EventBase>)Delegate.Combine(source, d);
                }
            else
            {
                LogSystem.Error("can't combine two deferent delegate!  source {0} target {1} evt {2}", source.GetType(), d.GetType(), evt.ToString());
            }
            else
                source = d;
        }
        else
        {
            source = d;
        }
        subscribers_[evt] = source;
        return new ReceiptInfo(evt, d);
    }

    public void Unsubscribe(object receipt)
    {
        ReceiptInfo r = receipt as ReceiptInfo;
        Action<string, EventBase> d;
        if (null != r && subscribers_.TryGetValue(r.name_, out d))
        {
            subscribers_[r.name_] = (Action<string, EventBase>)Delegate.Remove(d, r.delegate_);
        }
    }

    public void Publish(string key, EventBase ev = null)
    {
        try
        {
            Action<string, EventBase> d;
            if (subscribers_.TryGetValue(key, out d))
            {
                if (null == d)
                {
                    subscribers_.Remove(key);
                }
                else
                {
                    d(key, ev);
                }
            }
        }
        catch (Exception ex)
        {
            LogSystem.Error("SubscribeSystem.Publish event {0} err: {1} \n {2}", key, ex.Message, ex.StackTrace);
        }
    }

    private static GameEventor instance_ = new GameEventor();
    private Dictionary<string, Action<string, EventBase>> subscribers_ = new Dictionary<string, Action<string, EventBase>>();
}