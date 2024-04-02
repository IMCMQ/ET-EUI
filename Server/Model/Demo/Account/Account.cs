namespace ET
{
    public enum AccountType
    {
        General = 0,  //白名单类型
        
        BlackList = 1 //黑名单类型
    }

    public class Account: Entity, IAwake
    {
        public string AccountName; //账号名

        public string password;  //账号密码

        public long CreateTime;  //账号创建时间

        public int AccountType; //账号类型
    }
    
}       