using System.Collections.Generic;

public class Pool<T> where T : new()
{
 private int exist_count_ = 0; // 已创建并且还存在的数量
 private int unrecircle_count_ = 0; // 未被回收的数量
 private int cap_; // 最大容量
 private List<T> pool_ = new List<T>();

 public int pool_size() { return pool_.Count; }
 public int exist_count() { return exist_count_; }
 public int unrecircle_count() { return unrecircle_count_; }

 public Pool(int cap = 0)
 {
  cap_ = cap;
 }

 // 取得或新增
 public virtual T GetOrCreate()
 {
  T t = GetFromPool();
  if (t == null)
  {
   t = OnCreate();
   if (t != null)
   {
    exist_count_++;
    unrecircle_count_++;
   }
   return t;
  }
  unrecircle_count_++;
  OnGet(t);
  return t;
 }

 public virtual void Recycle(List<T> list)
 {
  for (int i = list.Count - 1; i >= 0; i--)
  {
   Recycle(list[i]);
  }
  list.Clear();
 }

 // 回收
 public virtual void Recycle(T t)
 {
  if (pool_.Contains(t))
  { // 防止重复回收
   return;
  }
  unrecircle_count_--;
  if (IsFull())
  {
   exist_count_--;
   OnDestroy(t);
   return;
  }
  pool_.Add(t);
  OnRecycle(t);
 }

 // 清空
 public virtual void Clear()
 {
  for (int i = 0; i < pool_.Count; i++)
  {
   exist_count_--;
   unrecircle_count_--;
   OnDestroy(pool_[i]);
  }
  pool_.Clear();
  OnClear();
 }

 // 创建新的对象
 public virtual T OnCreate()
 {
  return new T();
 }

 public virtual void OnGet(T t) { }

 public virtual void OnRecycle(T t) { }

 // 子类需要实现，销毁
 public virtual void OnDestroy(T t) { }

 // 池清除时的回调
 public virtual void OnClear() { }

 private T GetFromPool()
 {
  if (pool_.Count > 0)
  {
   T t = pool_[0];
   pool_.RemoveAt(0);
   return t;
  }
  return default(T);
 }

 private bool IsFull()
 {
  if (cap_ == 0)
  {
   return false;
  }
  return pool_.Count >= cap_;
 }
}