using System;
using System.Text.RegularExpressions;

namespace ET
{
    [FriendClass(typeof(Account))]
    public class C2A_LoginAccountHandler : AMRpcHandler<C2A_LoginAccount,A2C_LoginAccount>
    {
        protected override async ETTask Run(Session session, C2A_LoginAccount request, A2C_LoginAccount response, Action reply)
        {
            if (session.DomainScene().SceneType == SceneType.Account)
            {
                Log.Error($"请求的Scene错误，当前Scene为：{session.DomainScene().SceneType}");
            }
            
            session.RemoveComponent<SessionAcceptTimeoutComponent>();

            if (string.IsNullOrEmpty(request.AccountName) || string.IsNullOrEmpty(request.Password))
            {
                response.Error = ErrorCode.ERR_LoginInfoError;
                reply();
                session.Dispose();
                return;
            }

            if (!Regex.IsMatch(request.AccountName.Trim(), @"^(?=.*[0-9].*)(?=.*[A-Z].*)(?=.*[a-z].*).{6,15}$"))
            {
                response.Error = ErrorCode.ERR_LoginInfoError;
                reply();
                session.Dispose();
                return;
            }
            if (!Regex.IsMatch(request.Password.Trim(), @"^(?=.*[0-9].*)(?=.*[A-Z].*)(?=.*[a-z].*).{6,15}$"))
            {
                response.Error = ErrorCode.ERR_LoginInfoError;
                reply();
                session.Dispose();
                return;
            }

            var accountinfoList = await DBManagerComponent.Instance.GetZoneDB(session.DomainScene().DomainZone()).Query<Account>(d=>d.AccountName.Equals(request.AccountName.Trim()));
            Account account = null;
            if (accountinfoList.Count > 0)
            {
                account = accountinfoList[0];
                session.AddChild(account);
                if (account.AccountType = AccountType.BlackList)
                {
                    response.Error = ErrorCode.ERR_LoginInfoError;
                    reply();
                    session.Dispose();
                    return;

                }
                if (!account.password.Equals(request.Password))
                {
                    response.Error = ErrorCode.ERR_LoginInfoError;
                    reply();
                    session.Dispose();
                    return;

                }

            }
            else
            {
                account = session.AddChild<Account>();
                account.AccountName = request.AccountName.Trim();
                account.password = request.Password;
                account.CreateTime = TimeHelper.ServerNow();
                account.AccountType = (int)AccountType.General;

                await DBManagerComponent.Instance.GetZoneDB(session.DomainZone()).Save<Account>(account);
                
            }

            string Token = TimeHelper.ServerNow().ToString() + RandomHelper.RandomNumber(int.MinValue, int.MaxValue).ToString();
            
            
            await ETTask.CompletedTask;
        }
    }
}       