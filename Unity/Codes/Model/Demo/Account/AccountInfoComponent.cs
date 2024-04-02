namespace ET
{
    [ComponentOf(typeof(Scene))]
    public class AccountInfoComponent : Entity,IAwake,IDestroy
    {
        //用户令牌，令牌就是用于账号服务器的连接，就是验证码
        public string Token;
        //获取账户唯一标识AccountId
        public long AccountId;
    }
}