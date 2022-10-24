﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quiz.Site.Extensions;
using Quiz.Site.Helpers;
using Quiz.Site.Models.Badges;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace Quiz.Site.Services;
public class BadgeService : IBadgeService
{
    private readonly IMemberService _memberService;
    private readonly IUmbracoContextFactory _umbracoContextFactory;

    public BadgeService(IMemberService memberService, IUmbracoContextFactory umbracoContextFactory)
    {
        _memberService = memberService;
        _umbracoContextFactory = umbracoContextFactory;
    }
    
    public BadgePage? GetBadgeByName(string name)
    {
        using var umbracoContextReference = _umbracoContextFactory.EnsureUmbracoContext();
        var badges = ContentHelper.GetBadges(umbracoContextReference.UmbracoContext.Content!);
        return badges.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.CurrentCultureIgnoreCase));
    }

    public bool HasBadge(IMember member, BadgePage badge)
    {
        var udi = badge.GetUdiObject().ToString();
        
        if (member == null)
        {
            throw new Exception("Member is null");
        }

        var badgesValue = member.GetValue<string>("badges");
        var badgesArray = !string.IsNullOrWhiteSpace(badgesValue) ? JsonConvert.DeserializeObject<JArray>(badgesValue) : new JArray();;
        
        return badgesArray != null && badgesArray.Contains(udi);
    }
    

    public bool AddBadgeToMember(IMember member, IBadge badge, bool pushNotification = true)
    {
       var success =  AssignBadgeToMember(member, badge);
       if (success && pushNotification)
       {
           //TODO Push BadgeAssigned Notification to further handle user-visible notifications
       }

       return success;
    }

    private bool AssignBadgeToMember(IMember member, IBadge badge)
    {
        var badgeItem = GetBadgeByName(badge.UniqueUmbracoName);
        if (badgeItem is null)
        {
            return false;
        }

        if (HasBadge(member, badgeItem))
        {
            return false;
        }

        var badgesValue = member.GetValue<string>("badges");
        var badgesArray = !string.IsNullOrWhiteSpace(badgesValue) ? JsonConvert.DeserializeObject<JArray>(badgesValue) : new JArray();;
            
        badgesArray?.Add(badgeItem.GetUdiObject().ToString());
        member.SetValue("badges", badgesArray);
        
        _memberService.Save(member);
        
        return true;

    }
}
