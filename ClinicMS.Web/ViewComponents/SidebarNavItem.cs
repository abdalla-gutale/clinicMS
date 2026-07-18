namespace ClinicMS.Web.ViewComponents;

public record SidebarNavItem(string Name, string? Url, IReadOnlyList<SidebarNavItem> Children)
{
    public bool IsGroup => Children.Count > 0;
}

public record SidebarModule(string Name, string Icon, IReadOnlyList<SidebarNavItem> Items);
