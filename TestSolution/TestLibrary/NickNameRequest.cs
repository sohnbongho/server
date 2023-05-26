namespace TestLibrary
{
    /// <summary>
    /// 닉네임 요청
    /// </summary>
    public class NickNameRequest
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////// Property
        ////////////////////////////////////////////////////////////////////////////////////////// Public

        #region 사용자명 - UserName

        /// <summary>
        /// 사용자명
        /// </summary>
        public string UserName { get; set; }

        #endregion
        #region 신규 사용자명 - NewUserName

        /// <summary>
        /// 신규 사용자명
        /// </summary>
        public string NewUserName { get; set; }

        #endregion
    }
}