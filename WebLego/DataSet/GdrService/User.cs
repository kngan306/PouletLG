using System;
using System.Collections.Generic;

namespace WebLego.DataSet.GdrService;

public partial class User
{
    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Gender { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public string? UserPassword { get; set; }

    public int RoleId { get; set; }

    public string? UserStatus { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();

    public virtual CustomerProfile? CustomerProfile { get; set; }

    public virtual ICollection<HomeBanner> HomeBanners { get; set; } = new List<HomeBanner>();

    public virtual ICollection<Order> OrderShippers { get; set; } = new List<Order>();

    public virtual ICollection<Order> OrderUsers { get; set; } = new List<Order>();

    public virtual ICollection<ProductReturn> ProductReturns { get; set; } = new List<ProductReturn>();

    public virtual ICollection<ProductReview> ProductReviews { get; set; } = new List<ProductReview>();

    public virtual ICollection<Product> ProductsNavigation { get; set; } = new List<Product>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<UserAddress> UserAddresses { get; set; } = new List<UserAddress>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    // Thêm thuộc tính Favorites
    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

    // Thêm thuộc tính cho ContactInformations
    public virtual ICollection<ContactInformation> ContactInformations { get; set; }

    // Thêm thuộc tính cho AboutUsSection
    public virtual ICollection<AboutUsSection> AboutUsSections { get; set; } = new List<AboutUsSection>();

    public virtual ICollection<CommunityPost> CommunityPosts { get; set; } = new List<CommunityPost>();

    public virtual ICollection<CommunityComment> CommunityComments { get; set; } = new List<CommunityComment>();

    public virtual ICollection<ContestVote> ContestVotes { get; set; } = new List<ContestVote>();

    public virtual ICollection<Contest> Contests { get; set; } = new List<Contest>();
}
