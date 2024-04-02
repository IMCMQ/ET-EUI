namespace ET
{
    public class AccountSessionComponentDestroySystem: DestroySystem<AccountSessionsComponent>
    {
        public override void Destroy(AccountSessionsComponent self)
        {
            self.AccountSessionDictionary.Clear();
        }
    }
    
    [FriendClass(typeof(AccountSessionsComponent))]
    public static class AccountSessionsComponentSystem
    {
        public static long Get(this AccountSessionsComponent self, long accountId)
        {
            if (!self.AccountSessionDictionary.TryGetValue(accountId, out long intanceId))
            {
                return 0;
            }

            return intanceId;
        }
        
        public static void Add(this AccountSessionsComponent self, long accountId,long sessionInstanceId)
        {
            if (!self.AccountSessionDictionary.ContainsKey(accountId))
            {
                self.AccountSessionDictionary[accountId] = sessionInstanceId;
                return ;
            }
            self.AccountSessionDictionary.Add(accountId,sessionInstanceId);
        }
        
        public static void Remove(this AccountSessionsComponent self, long accountId)
        {
            if (!self.AccountSessionDictionary.ContainsKey(accountId))
            {
                self.AccountSessionDictionary.Remove(accountId);
                return ;
            }
            
        }
    }
}