namespace ET
{
    public static class DisconnectHelper
    {
        public static async ETTask Disconnect(this Session self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            //临时保存instanceId
            long instanceId = self.InstanceId;

            await TimerComponent.Instance.WaitAsync(1000);

            //1秒后，此时session的instanceId和之前保存的不相同
            //那么代表已经被释放和重建，如果继续Dispose就会出现逻辑错误
            //释放session会将instanceId置0
            if (self.InstanceId != instanceId)
            {
                return;
            }
            //安全释放
            self.Dispose();
        }
    }
}