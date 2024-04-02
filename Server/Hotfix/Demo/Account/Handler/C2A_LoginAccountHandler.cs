using System;
using System.Text.RegularExpressions;

namespace ET
{
    [FriendClass(typeof(Account))]
    public class C2A_LoginAccountHandler : AMRpcHandler<C2A_LoginAccount,A2C_LoginAccount>
    {
        protected override async ETTask Run(Session session, C2A_LoginAccount request, A2C_LoginAccount response, Action reply)
        {
            //判断请求的SceneType（进程）是否为Account
            if (session.DomainScene().SceneType != SceneType.Account)
            {
                Log.Error($"请求的Scene错误，当前Scene为：{session.DomainScene().SceneType}");
                session.Dispose();
                return;
            }
            //卸载掉计时器断开的组件（避免自动断开连接）代表我们的连接通过验证
            session.RemoveComponent<SessionAcceptTimeoutComponent>();

            //判断自身是否存在SessionLockingComponent组件，用于避免重复登录操作
            if (session.GetComponent<SessionLockingComponent>() != null)
            {
                response.Error = ErrorCode.ERR_RequestRepeatedly;
                reply();
                session.Disconnect().Coroutine();
                return;
            }
            
            //判断账号密码的不为空
            if (string.IsNullOrEmpty(request.AccountName) || string.IsNullOrEmpty(request.Password))
            {
                response.Error = ErrorCode.ERR_LoginInfoIsNull;
                reply();
                session.Disconnect().Coroutine();
                return;
            }
            //正则表达式，判断账号格式是否输入正确
            if (!Regex.IsMatch(request.AccountName.Trim(), @"^(?=.*[0-9].*)(?=.*[A-Z].*)(?=.*[a-z].*).{6,15}$"))
            {
                response.Error = ErrorCode.ERR_AccountNameFormtError;
                reply();
                session.Disconnect().Coroutine();
                return;
            }
            //正则表达式，判断密码格式是否输入正确
            if (!Regex.IsMatch(request.Password.Trim(), @"^(?=.*[0-9].*)(?=.*[A-Z].*)(?=.*[a-z].*).{6,15}$"))
            {
                response.Error = ErrorCode.ERR_PasswordFormtError;
                reply();
                session.Disconnect().Coroutine();
                return;
            }

            //using关键字:执行完后自动释放SessionLockingComponent组件
            using (session.AddComponent<SessionLockingComponent>())
            {
                //协程锁：访问公共的资源（全局变量），防止同时登录执行（不同玩家同一个请求消息处理），需要等待执行完其中一位玩家登录再执行下一位玩家登录
                //使用协程锁，锁的是异步逻辑进入这个异步逻辑，就会锁上，直到执行完，才会解锁，让下个逻辑进来
                //同时这个协程锁必须是唯一的，我们得有唯一的id进行标识，那么此时session登录消息的账户名的hash值，就是最好的id
                using (await CoroutineLockComponent.Instance.Wait(CoroutineLockType.LoginAccount,request.AccountName.Trim().GetHashCode()))
                {
                    //通过账号服务器查询是否存在账号
                    var accountinfoList = await DBManagerComponent.Instance.GetZoneDB(session.DomainScene().DomainZone())
                            .Query<Account>(d => d.AccountName.Equals(request.AccountName.Trim()));
                    Account account = null;
                    if (accountinfoList!=null && accountinfoList.Count > 0)
                    {
                        //存在账号进行账号信息校验
                        account = accountinfoList[0];
                        session.AddChild(account);
                        //判断是否在黑名单
                        if (account.AccountType == (int)AccountType.BlackList)
                        {
                            response.Error = ErrorCode.ERR_AccountInBlackListError;
                            reply();
                            session.Disconnect().Coroutine();    
                            account?.Dispose();
                            return;

                        }
                        //校验登录的账号密码
                        if (!account.password.Equals(request.Password))
                        {
                            response.Error = ErrorCode.ERR_LoginPasswordError;
                            reply();
                            session.Disconnect().Coroutine();
                            account?.Dispose();
                            return;

                        }

                    }
                    else
                    {
                        //不存在该账号则注册账号
                        account = session.AddChild<Account>();
                        account.AccountName = request.AccountName.Trim();
                        account.password = request.Password;
                        account.CreateTime = TimeHelper.ServerNow();
                        account.AccountType = (int)AccountType.General;
                        //保存账号密码到账号服务器数据库
                        await DBManagerComponent.Instance.GetZoneDB(session.DomainZone()).Save<Account>(account);

                    }

                    long accountSessionInstanceId = session.DomainScene().GetComponent<AccountSessionsComponent>().Get(account.Id);
                    Session otherSession = Game.EventSystem.Get(accountSessionInstanceId) as Session;
                    otherSession?.Send(new A2C_Disconnect(){Error = 0});
                    otherSession?.Disconnect().Coroutine();
                    session.DomainScene().GetComponent<AccountSessionsComponent>().Add(account.Id,session.InstanceId);
                    
                    //生成用户令牌
                    string Token = TimeHelper.ServerNow().ToString() + RandomHelper.RandomNumber(int.MinValue, int.MaxValue).ToString();
                    session.DomainScene().GetComponent<TokenComponent>().Remove(account.Id);
                    session.DomainScene().GetComponent<TokenComponent>().Add(account.Id, Token);

                    //给客户端返回令牌和账号id
                    response.AccountId = account.Id;
                    response.Token = Token;

                    reply();
                    
                    account?.Dispose();
                }
               

            }
        }
    }
}       