#GroupDateExport

##Introduction
Export your sites group data securely into a csv file

##Developer:

New fields can be added or removed in the 'BuildHeader' method.
        List<string> elements = new List<string>
        {
            "GroupName",
            "GroupDescription",
            "GroupCreated",
            "GroupLastActivity",
            "UserName",
            "GroupOwner",
            "GroupManager",
            "GroupMembershipType",
            "DisplayName",
            "Private Email",
            "Public Email",
            "LastLoginDate"
        };
  
Data can be added using the 'ExtractGroupUser' method:
        List<string> elements = new List<string>
        {
            groupUser.Group.Name,
            groupUser.Group.Description,
            Apis.Get<ILanguage>().FormatDateAndTime(groupUser.Group.DateCreated.GetValueOrDefault(DateTime.MinValue)),
            Apis.Get<ILanguage>().FormatDateAndTime(latestPostDateTime),
            groupUser.User.Username,
            groupUser.MembershipType.Contains("Owner") ? "Yes": "",
            groupUser.MembershipType.Contains("Manager") ? "Yes": "",
            groupUser.MembershipType,
            groupUser.User.DisplayName,
            groupUser.User.PrivateEmail,
            groupUser.User.PublicEmail,
            Apis.Get<ILanguage>().FormatDateAndTime(groupUser.User.LastLoginDate.GetValueOrDefault(DateTime.MinValue))
        };
