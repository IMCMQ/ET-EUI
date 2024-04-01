namespace ET
{
    [ComponentOf(typeof(Scene))]
    public class AccountInfoComponent : Entity,IAwake,IDestroy
    {
        //令牌，
        public string Token;
        //获取账户唯一标识AccountId
        public long AccountId;
    }
}