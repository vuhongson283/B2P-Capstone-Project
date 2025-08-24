import React, { useState, useEffect } from "react";
import { useAuth } from "../../contexts/AuthContext";
import {
  Card,
  Button,
  Input,
  Modal,
  Form,
  message,
  Avatar,
  Dropdown,
  Upload,
  Image,
  Spin,
  Empty,
  Pagination,
  Tooltip,
  Space,
  Tag,
  Divider,
  Select,
} from "antd";
import {
  PlusOutlined,
  EditOutlined,
  DeleteOutlined,
  MoreOutlined,
  MessageOutlined,
  PictureOutlined,
  UserOutlined,
  CalendarOutlined,
  ClockCircleOutlined,
  ExclamationCircleOutlined,
  InboxOutlined,
  CloseOutlined,
  SearchOutlined,
  SortAscendingOutlined,
  SortDescendingOutlined,
  FilterOutlined,
  ClearOutlined,
  MenuFoldOutlined,
  MenuUnfoldOutlined,
  LeftOutlined,
  RightOutlined,
} from "@ant-design/icons";
import {
  getAllBlogs,
  getBlogById,
  createBlog,
  updateBlog,
  deleteBlog,
  uploadBlogImage,
  getBlogImages,
  deleteImage,
  getUserById,
  getUserImage
} from "../../services/apiService";
import "./Blog.scss";
import CommentModal from "../Common/CommentModal";
import BlogModal from "../Common/BlogModal";

const { TextArea } = Input;
const { Search } = Input;
const { Option } = Select;


const Blog = () => {
  // =============== STATE MANAGEMENT ===============
  const [blogs, setBlogs] = useState([]);
  const [loading, setLoading] = useState(false);

  // Blog images cache state
  const [blogImagesCache, setBlogImagesCache] = useState({});
  const [loadingBlogImages, setLoadingBlogImages] = useState({});

  // Current user info
  const { user, isLoggedIn, isLoading: authLoading } = useAuth();
  const convertGoogleDriveUrl = (url) => {
  if (!url) return "";
  const fileIdMatch = url.match(/[?&]id=([a-zA-Z0-9-_]+)/);
  if (fileIdMatch) {
    const fileId = fileIdMatch[1];
    return `https://drive.google.com/thumbnail?id=${fileId}&sz=w800`;
  }
  return url;
};
const convertGoogleDriveLink = (driveUrl) => {
  if (!driveUrl) return null;
  
  // Check if it's a Google Drive link
  if (driveUrl.includes('drive.google.com')) {
    // Extract file ID from different Google Drive URL formats
    let fileId = null;
    
    // Format 1: https://drive.google.com/file/d/FILE_ID/view?usp=sharing
    const match1 = driveUrl.match(/\/file\/d\/([a-zA-Z0-9_-]+)/);
    if (match1) {
      fileId = match1[1];
    }
    
    // Format 2: https://drive.google.com/open?id=FILE_ID
    const match2 = driveUrl.match(/[?&]id=([a-zA-Z0-9_-]+)/);
    if (match2) {
      fileId = match2[1];
    }
    
    // Convert to googleusercontent format - MUCH BETTER!
    if (fileId) {
      return `https://lh3.googleusercontent.com/d/${fileId}=s400-c`;
    }
  }
  
  // If not Google Drive link, return as is
  return driveUrl;
};
useEffect(() => {
  const loadUserAvatar = async () => {
    if (user?.userId) {
      try {
        const response = await getUserImage(user.userId);
        if (response.data) {
          const convertedAvatarUrl = convertGoogleDriveLink(response.data);
          setCurrentUser(prev => ({
            ...prev,
            avatar: convertedAvatarUrl || prev.avatar
          }));
        }
      } catch (error) {
        console.error('❌ Error loading user avatar:', error);
      }
    }
  };

  loadUserAvatar();
}, [user?.userId]);
const [currentUser, setCurrentUser] = useState({
  userId: user?.userId,
  fullName: user?.fullName || "",
  userName: user?.userName || "Người dùng",
  avatar: user?.avatar ||
    "https://ui-avatars.com/api/?name=Người dùng&background=27ae60&color=fff&size=200",
  roleId: user?.roleId || 2,
});
  useEffect(() => {
    document.title = "Bài viết - B2P";
  }, []);
  // User cache for displaying other users
  const [userCache, setUserCache] = useState({
    [currentUser.userId]: currentUser,
  });

  // Pagination & Search/Sort/Filter
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize] = useState(4);
  const [totalBlogs, setTotalBlogs] = useState(0);
  const [searchTerm, setSearchTerm] = useState("");
  const [sortBy, setSortBy] = useState("postAt");
  const [sortDirection, setSortDirection] = useState("desc");
  const [isSearching, setIsSearching] = useState(false);
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);

  // Blog Modal states
  const [blogModalVisible, setBlogModalVisible] = useState(false);
  const [editingBlog, setEditingBlog] = useState(null);

  // Comment Modal states
  const [commentModalVisible, setCommentModalVisible] = useState(false);
  const [currentBlogForComments, setCurrentBlogForComments] = useState(null);
  const [blogCommentCounts, setBlogCommentCounts] = useState({});

  // Image modal states
  const [imageModalVisible, setImageModalVisible] = useState(false);
  const [selectedImages, setSelectedImages] = useState([]);
  const [currentImageIndex, setCurrentImageIndex] = useState(0);

  // =============== HELPER FUNCTIONS ===============
  const formatDateTime = (dateString) => {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now - date;
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return "Vừa xong";
    if (diffMins < 60) return `${diffMins} phút trước`;
    if (diffHours < 24) return `${diffHours} giờ trước`;
    if (diffDays < 7) return `${diffDays} ngày trước`;

    return date.toLocaleDateString("vi-VN", {
      year: "numeric",
      month: "short",
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  };

  const getUserInfo = async (userId) => {
    if (userCache[userId]) {
      return userCache[userId];
    }

    try {
      const response = await getUserById(userId);
      if (response && response.success) {
        const userInfo = {
          userId: response.data.userId,
          fullName: response.data.fullName || `User ${userId}`,
          userName:
            response.data.userName ||
            response.data.fullName ||
            `User ${userId}`,
          avatar: `https://ui-avatars.com/api/?name=${encodeURIComponent(
            response.data.fullName || `User ${userId}`
          )}&background=27ae60&color=fff&size=200`,
          roleId: response.data.roleId,
        };

        setUserCache((prev) => ({ ...prev, [userId]: userInfo }));
        return userInfo;
      }
    } catch (error) {
      console.error("Error fetching user info:", error);
    }

    const fallbackUser = {
      userId: userId,
      fullName: `User ${userId}`,
      userName: `User ${userId}`,
      avatar: `https://ui-avatars.com/api/?name=User${userId}&background=6c757d&color=fff&size=200`,
      roleId: 2,
    };

    setUserCache((prev) => ({ ...prev, [userId]: fallbackUser }));
    return fallbackUser;
  };

  const fetchBlogImagesForCard = async (blogId) => {
    if (blogImagesCache[blogId]) {
      return blogImagesCache[blogId];
    }

    if (loadingBlogImages[blogId]) {
      return [];
    }

    try {
      setLoadingBlogImages((prev) => ({ ...prev, [blogId]: true }));

      const response = await getBlogImages(blogId);

      let imagesData = null;

      if (Array.isArray(response)) {
        imagesData = response;
      } else if (response && response.success && Array.isArray(response.data)) {
        imagesData = response.data;
      } else if (response && Array.isArray(response.data)) {
        imagesData = response.data;
      }

      if (imagesData && imagesData.length > 0) {
        const images = imagesData
          .sort((a, b) => (a.order || 0) - (b.order || 0))
          .map((image) => ({
            ...image,
            imageUrl: image.imageUrl,
          }));

        setBlogImagesCache((prev) => ({
          ...prev,
          [blogId]: images,
        }));

        return images;
      }
    } catch (error) {
      console.error(`Error fetching images for blog ${blogId}:`, error);
    } finally {
      setLoadingBlogImages((prev) => ({ ...prev, [blogId]: false }));
    }

    return [];
  };

  // =============== EFFECTS ===============
  useEffect(() => {
    fetchBlogs();
  }, [currentPage, sortBy, sortDirection]);

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      if (searchTerm !== "" || isSearching) {
        setCurrentPage(1);
        fetchBlogs();
      }
    }, 500);

    return () => clearTimeout(timeoutId);
  }, [searchTerm]);

  useEffect(() => {
    if (blogs.length > 0) {
      blogs.forEach((blog) => {
        fetchBlogImagesForCard(blog.blogId);
      });
    }
  }, [blogs]);

  // =============== BLOG FUNCTIONS ===============
  const fetchBlogs = async () => {
    try {
      setLoading(true);

      const queryParams = {
        search: searchTerm.trim() || undefined,
        page: currentPage,
        pageSize: pageSize,
        sortBy: sortBy,
        sortDirection: sortDirection,
      };

      Object.keys(queryParams).forEach(
        (key) => queryParams[key] === undefined && delete queryParams[key]
      );

      const response = await getAllBlogs(queryParams);

      if (response && response.success) {
        const blogsData = response.data.items || [];
        setBlogs(blogsData);
        setTotalBlogs(response.data.totalItems || 0);

        const userIds = [...new Set(blogsData.map((blog) => blog.userId))];
        userIds.forEach((userId) => {
          if (!userCache[userId]) {
            getUserInfo(userId);
          }
        });

        setIsSearching(searchTerm.trim() !== "");
      }
    } catch (error) {
      console.error("Error fetching blogs:", error);
      message.error("Không thể tải danh sách blog");
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = (value) => {
    setSearchTerm(value);
  };

  const handleSortChange = (newSortBy) => {
    if (newSortBy === sortBy) {
      setSortDirection((prev) => (prev === "asc" ? "desc" : "asc"));
    } else {
      setSortBy(newSortBy);
      setSortDirection("desc");
    }
    setCurrentPage(1);
  };

  const handleClearFilters = () => {
    setSearchTerm("");
    setSortBy("postAt");
    setSortDirection("desc");
    setCurrentPage(1);
    setIsSearching(false);
  };

  const toggleSidebar = () => {
    setSidebarCollapsed(!sidebarCollapsed);
  };

  const openBlogModal = (blog = null) => {
    setEditingBlog(blog);
    setBlogModalVisible(true);
  };

  const closeBlogModal = () => {
    setBlogModalVisible(false);
    setEditingBlog(null);
  };

  const handleBlogSuccess = () => {
    fetchBlogs();
    closeBlogModal();
  };

  const openCommentModal = async (blog) => {
    const updatedBlog = {
      ...blog,
      totalComments: blogCommentCounts[blog.blogId] ?? blog.totalComments ?? 0,
    };

    setCurrentBlogForComments(updatedBlog);
    setCommentModalVisible(true);
  };

  const closeCommentModal = () => {
    setCommentModalVisible(false);
    setCurrentBlogForComments(null);
  };

  const handleCommentCountUpdate = (blogId, newCount) => {
    setBlogCommentCounts((prev) => ({
      ...prev,
      [blogId]: newCount,
    }));

    setBlogs((prevBlogs) =>
      prevBlogs.map((blog) =>
        blog.blogId === blogId ? { ...blog, totalComments: newCount } : blog
      )
    );

    if (currentBlogForComments && currentBlogForComments.blogId === blogId) {
      setCurrentBlogForComments((prev) => ({
        ...prev,
        totalComments: newCount,
      }));
    }
  };

  const handleDeleteBlog = (blog) => {
    Modal.confirm({
      title: "🗑️ Xác nhận xóa bài viết",
      content: (
        <div>
          <p>Bạn có chắc chắn muốn xóa bài viết này không?</p>
          <div
            style={{
              background: "#f8f9fa",
              padding: "12px",
              borderRadius: "8px",
              marginTop: "12px",
            }}
          >
            <strong>"{blog.title}"</strong>
            <div style={{ fontSize: "12px", color: "#666", marginTop: "4px" }}>
              ⚠️ Hành động này sẽ xóa bài viết và tất cả ảnh đính kèm!
            </div>
          </div>
        </div>
      ),
      okText: "Xóa bài viết",
      cancelText: "Hủy",
      okType: "danger",
      onOk: async () => {
        try {
          const blogImages = await fetchBlogImagesForCard(blog.blogId);

          if (blogImages && blogImages.length > 0) {
            for (const image of blogImages) {
              try {
                await deleteImage(image.imageId);
              } catch (error) {
                console.error(`Error deleting image ${image.imageId}:`, error);
              }
            }
          }

          const response = await deleteBlog(
            blog.blogId,
            currentUser.userId,
            currentUser.roleId
          );

          if (response && response.success) {
            message.success("🗑️ Xóa bài viết thành công!");

            setBlogImagesCache((prev) => {
              const newCache = { ...prev };
              delete newCache[blog.blogId];
              return newCache;
            });

            fetchBlogs();
          } else {
            message.error(response?.message || "Không thể xóa bài viết");
          }
        } catch (error) {
          console.error("Error deleting blog:", error);
          message.error("Có lỗi khi xóa bài viết");
        }
      },
    });
  };

  // =============== RENDER FUNCTIONS ===============
  const renderBlogActions = (blog) => {
    const canEdit =
      blog.userId === currentUser.userId || currentUser.roleId === 1;

    const items = [
      ...(canEdit
        ? [
            {
              key: "edit",
              label: (
                <span>
                  <EditOutlined style={{ marginRight: 8 }} />
                  Chỉnh sửa
                </span>
              ),
              onClick: () => openBlogModal(blog),
            },
            {
              key: "delete",
              label: (
                <span>
                  <DeleteOutlined style={{ marginRight: 8 }} />
                  Xóa bài viết
                </span>
              ),
              danger: true,
              onClick: () => handleDeleteBlog(blog),
            },
          ]
        : []),
    ];

    return items.length > 0 ? (
      <Dropdown menu={{ items }} trigger={["click"]} placement="bottomRight">
        <Button type="text" icon={<MoreOutlined />} />
      </Dropdown>
    ) : null;
  };

  const renderUserAvatar = (userId, size = "default") => {
    const user = userCache[userId];

    return (
      <div className="user-avatar-wrapper">
        <Avatar
          src={user?.avatar}
          size={size}
          style={{
            border: "1px solid #e9ecef",
            cursor: "pointer",
          }}
        >
          {user?.fullName?.charAt(0) || "U"}
        </Avatar>
      </div>
    );
  };

  // 🎯 FACEBOOK-STYLE IMAGE LAYOUT FUNCTION
  const renderFacebookImageLayout = (images) => {
    const imageCount = images.length;

    if (imageCount === 0) return null;

    // Single image - full width
    if (imageCount === 1) {
      const convertedUrl = convertGoogleDriveUrl(images[0].imageUrl);
      return (
        <div className="fb-image-layout single">
          <div className="fb-image-container">
            <Image
              src={convertedUrl}
              alt={images[0].caption || "Blog image"}
              style={{
                width: "100%",
                maxHeight: "500px",
                objectFit: "cover",
                borderRadius: "8px",
              }}
              preview={{
                mask: <div className="preview-mask">👁️ Xem ảnh</div>,
              }}
              fallback="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="
            />
          </div>
        </div>
      );
    }

    // Two images - side by side
    if (imageCount === 2) {
      return (
        <div className="fb-image-layout two-images">
          {images.slice(0, 2).map((image, index) => {
            const convertedUrl = convertGoogleDriveUrl(image.imageUrl);
            return (
              <div
                key={image.imageId || index}
                className="fb-image-container half"
              >
                <Image
                  src={convertedUrl}
                  alt={image.caption || `Blog image ${index + 1}`}
                  style={{
                    width: "100%",
                    height: "300px",
                    objectFit: "cover",
                    borderRadius: "8px",
                  }}
                  preview={{
                    mask: <div className="preview-mask">👁️ Xem ảnh</div>,
                  }}
                  fallback="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="
                />
              </div>
            );
          })}
        </div>
      );
    }

    // Three images - 1 large left, 2 small right
    if (imageCount === 3) {
      const mainImage = convertGoogleDriveUrl(images[0].imageUrl);
      return (
        <div className="fb-image-layout three-images">
          <div className="fb-image-container main">
            <Image
              src={mainImage}
              alt={images[0].caption || "Blog image 1"}
              style={{
                width: "100%",
                height: "400px",
                objectFit: "cover",
                borderRadius: "8px",
              }}
              preview={{
                mask: <div className="preview-mask">👁️ Xem ảnh</div>,
              }}
              fallback="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="
            />
          </div>
          <div className="fb-image-sidebar">
            {images.slice(1, 3).map((image, index) => {
              const convertedUrl = convertGoogleDriveUrl(image.imageUrl);
              return (
                <div
                  key={image.imageId || index}
                  className="fb-image-container small"
                >
                  <Image
                    src={convertedUrl}
                    alt={image.caption || `Blog image ${index + 2}`}
                    style={{
                      width: "100%",
                      height: "195px",
                      objectFit: "cover",
                      borderRadius: "8px",
                    }}
                    preview={{
                      mask: <div className="preview-mask">👁️ Xem ảnh</div>,
                    }}
                    fallback="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="
                  />
                </div>
              );
            })}
          </div>
        </div>
      );
    }

    // Four images - 1 large left, 3 small right
    if (imageCount === 4) {
      const mainImage = convertGoogleDriveUrl(images[0].imageUrl);
      return (
        <div className="fb-image-layout four-images">
          <div className="fb-image-container main">
            <Image
              src={mainImage}
              alt={images[0].caption || "Blog image 1"}
              style={{
                width: "100%",
                height: "400px",
                objectFit: "cover",
                borderRadius: "8px",
              }}
              preview={{
                mask: <div className="preview-mask">👁️ Xem ảnh</div>,
              }}
              fallback="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="
            />
          </div>
          <div className="fb-image-sidebar">
            {images.slice(1, 4).map((image, index) => {
              const convertedUrl = convertGoogleDriveUrl(image.imageUrl);
              return (
                <div
                  key={image.imageId || index}
                  className="fb-image-container tiny"
                >
                  <Image
                    src={convertedUrl}
                    alt={image.caption || `Blog image ${index + 2}`}
                    style={{
                      width: "100%",
                      height: "127px",
                      objectFit: "cover",
                      borderRadius: "8px",
                    }}
                    preview={{
                      mask: <div className="preview-mask">👁️ Xem ảnh</div>,
                    }}
                    fallback="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="
                  />
                </div>
              );
            })}
          </div>
        </div>
      );
    }

    // Five or more images - 1 large left, 2 small right + overlay with count
    if (imageCount >= 5) {
      const mainImage = convertGoogleDriveUrl(images[0].imageUrl);
      const remainingCount = imageCount - 3;

      return (
        <div className="fb-image-layout five-plus-images">
          <div className="fb-image-container main">
            <Image
              src={mainImage}
              alt={images[0].caption || "Blog image 1"}
              style={{
                width: "100%",
                height: "400px",
                objectFit: "cover",
                borderRadius: "8px",
              }}
              preview={{
                mask: <div className="preview-mask">👁️ Xem ảnh</div>,
              }}
              fallback="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="
            />
          </div>
          <div className="fb-image-sidebar">
            {images.slice(1, 3).map((image, index) => {
              const convertedUrl = convertGoogleDriveUrl(image.imageUrl);
              const isLast = index === 1; // Last image in sidebar

              return (
                <div
                  key={image.imageId || index}
                  className="fb-image-container small"
                >
                  <div className="image-wrapper">
                    <Image
                      src={convertedUrl}
                      alt={image.caption || `Blog image ${index + 2}`}
                      style={{
                        width: "100%",
                        height: "195px",
                        objectFit: "cover",
                        borderRadius: "8px",
                      }}
                      preview={{
                        mask: <div className="preview-mask">👁️ Xem ảnh</div>,
                      }}
                      fallback="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="
                    />
                    {isLast && remainingCount > 0 && (
                      <div
                        className="more-images-overlay"
                        onClick={(e) => {
                          e.stopPropagation();
                          // Find the blog and open comment modal to see all images
                          const blogEntry = Object.entries(
                            blogImagesCache
                          ).find(
                            ([blogId, cachedImages]) => cachedImages === images
                          );
                          if (blogEntry) {
                            const blogId = parseInt(blogEntry[0]);
                            const blog = blogs.find((b) => b.blogId === blogId);
                            if (blog) {
                              openCommentModal(blog);
                            }
                          }
                        }}
                      >
                        <div className="overlay-content">
                          <span className="more-count">+{remainingCount}</span>
                          <span className="more-text">ảnh khác</span>
                        </div>
                      </div>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      );
    }

    return null;
  };

  // 🎯 MAIN RENDER BLOG IMAGES FUNCTION (FIXED - Only one version)
  const renderBlogImages = (images, isInModal = false) => {
    if (!images || images.length === 0) {
      return null;
    }

    if (isInModal) {
      return (
        <div className="blog-images-display modal-images">
          {images.map((image, index) => {
            const convertedUrl = convertGoogleDriveUrl(image.imageUrl);
            return (
              <div key={image.imageId || index} className="blog-image-item">
                <Image
                  src={convertedUrl}
                  alt={image.caption || `Blog image ${index + 1}`}
                  style={{
                    borderRadius: 12,
                    marginBottom: 12,
                    maxHeight: 400,
                    objectFit: "cover",
                    width: "100%",
                  }}
                  fallback="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="
                />
              </div>
            );
          })}
        </div>
      );
    }

    // 🎯 Facebook-style image layout for blog cards
    return (
      <div className="blog-images-display facebook-style">
        {renderFacebookImageLayout(images)}
      </div>
    );
  };

  const renderBlogCardImages = (blog) => {
    const images = blogImagesCache[blog.blogId] || [];
    const isLoading = loadingBlogImages[blog.blogId];

    if (isLoading) {
      return (
        <div className="blog-images-loading">
          <Spin size="small" />
          <span style={{ marginLeft: 8, fontSize: "12px", color: "#999" }}>
            Đang tải ảnh...
          </span>
        </div>
      );
    }

    if (images.length === 0) {
      return null;
    }

    return (
      <div
        onClick={(e) => {
          if (
            e.target.closest(".image-overlay") ||
            e.target.closest(".more-images-overlay")
          ) {
            openCommentModal(blog);
          }
        }}
        style={{ cursor: "pointer" }}
      >
        {renderBlogImages(images, false)}
      </div>
    );
  };

  const renderSidebar = () => (
    <div className={`fixed-sidebar ${sidebarCollapsed ? "collapsed" : ""}`}>
      <Card className="sidebar-card">
        <div className="sidebar-header">
          <Button
            type="text"
            icon={
              sidebarCollapsed ? <MenuUnfoldOutlined /> : <MenuFoldOutlined />
            }
            onClick={toggleSidebar}
            className="sidebar-toggle"
          />
          {!sidebarCollapsed && <h3>🔍 Tìm kiếm & Lọc</h3>}
        </div>

        {!sidebarCollapsed && (
          <div className="sidebar-content">
            <div className="sidebar-section">
              <h4>
                <SearchOutlined /> Tìm kiếm
              </h4>
              <Search
                placeholder="Tìm kiếm bài viết..."
                allowClear
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                onSearch={handleSearch}
              />
            </div>

            <Divider />

            <div className="sidebar-section">
              <h4>
                <FilterOutlined /> Sắp xếp theo
              </h4>

              <Select
                value={sortBy}
                onChange={handleSortChange}
                style={{ width: "100%", marginBottom: "12px" }}
                placeholder="Chọn cách sắp xếp"
              >
                <Option value="postAt">
                  <CalendarOutlined style={{ marginRight: 8 }} />
                  Ngày đăng
                </Option>
                <Option value="commentTime">
                  <MessageOutlined style={{ marginRight: 8 }} />
                  Hoạt động gần nhất
                </Option>
              </Select>

              <Button
                block
                onClick={() =>
                  setSortDirection((prev) => (prev === "asc" ? "desc" : "asc"))
                }
                type={sortDirection === "desc" ? "primary" : "default"}
                icon={
                  sortDirection === "asc" ? (
                    <SortAscendingOutlined />
                  ) : (
                    <SortDescendingOutlined />
                  )
                }
              >
                {sortDirection === "desc" ? "Mới nhất trước" : "Cũ nhất trước"}
              </Button>
            </div>

            <Divider />

            {(searchTerm ||
              sortBy !== "postAt" ||
              sortDirection !== "desc") && (
              <div className="sidebar-section">
                <h4>
                  <FilterOutlined /> Bộ lọc đang áp dụng
                </h4>

                <div className="filter-tags">
                  {searchTerm && (
                    <Tag
                      closable
                      onClose={() => setSearchTerm("")}
                      color="blue"
                    >
                      🔍 "{searchTerm}"
                    </Tag>
                  )}

                  {sortBy !== "postAt" && (
                    <Tag
                      closable
                      onClose={() => setSortBy("postAt")}
                      color="green"
                    >
                      📊{" "}
                      {sortBy === "commentTime"
                        ? "Hoạt động gần nhất"
                        : "Ngày đăng"}
                    </Tag>
                  )}

                  {sortDirection !== "desc" && (
                    <Tag
                      closable
                      onClose={() => setSortDirection("desc")}
                      color="orange"
                    >
                      ⬆️ Cũ nhất trước
                    </Tag>
                  )}
                </div>

                <Button
                  block
                  icon={<ClearOutlined />}
                  onClick={handleClearFilters}
                  type="dashed"
                  danger
                  style={{ marginTop: "12px" }}
                >
                  Xóa tất cả bộ lọc
                </Button>
              </div>
            )}

            {isSearching && (
              <>
                <Divider />
                <div className="sidebar-section">
                  <div
                    className={`search-result-info ${
                      totalBlogs > 0 ? "success" : "error"
                    }`}
                  >
                    {totalBlogs > 0
                      ? `🔍 Tìm thấy ${totalBlogs} bài viết`
                      : `❌ Không có kết quả`}
                  </div>
                </div>
              </>
            )}

            <Divider />
            <div className="sidebar-section">
              <h4>📊 Thống kê</h4>
              <div className="stats-info">
                <div>
                  📝 Tổng bài viết: <strong>{totalBlogs}</strong>
                </div>
                <div>
                  📄 Trang hiện tại: <strong>{currentPage}</strong>
                </div>
              </div>
            </div>
          </div>
        )}
      </Card>
    </div>
  );

  // =============== MAIN RENDER ===============
  return (
    <div className="blog-page-fixed-layout">
      {/* Fixed Sidebar */}
      {renderSidebar()}

      {/* Main Content Area */}
      <div
        className={`main-content-area ${
          sidebarCollapsed ? "sidebar-collapsed" : ""
        }`}
      >
        {/* Ultra Compact Header */}
        <div className="blog-header-ultra-compact">
          <div className="header-content">
            <h1>🏃‍♂️ Book2Play</h1>
            <p>Cộng đồng thể thao</p>
          </div>
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={() => openBlogModal()}
            className="create-blog-btn"
          >
            Tạo bài viết
          </Button>
        </div>

        {/* Mobile Search Bar */}
        <div className="mobile-search">
          <Card>
            <Search
              placeholder="Tìm kiếm bài viết..."
              allowClear
              enterButton={<SearchOutlined />}
              size="large"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              onSearch={handleSearch}
            />
          </Card>
        </div>

        {/* Blog List */}
        <div className="blog-list-container">
          <Spin
            spinning={loading}
            tip={isSearching ? "Đang tìm kiếm..." : "Đang tải bài viết..."}
          >
            {blogs.length === 0 ? (
              <Empty
                description={
                  isSearching
                    ? `Không tìm thấy bài viết nào với từ khóa "${searchTerm}"`
                    : "Chưa có bài viết nào"
                }
                image={Empty.PRESENTED_IMAGE_SIMPLE}
                imageStyle={{ height: 60 }}
              >
                {!isSearching && (
                  <Button
                    type="primary"
                    icon={<PlusOutlined />}
                    onClick={() => openBlogModal()}
                  >
                    Tạo bài viết đầu tiên
                  </Button>
                )}
              </Empty>
            ) : (
              blogs.map((blog) => {
                const blogUser = userCache[blog.userId];
                const isMyBlog = blog.userId === currentUser.userId;

                return (
                  <Card key={blog.blogId} className="blog-card">
                    {/* Blog Header */}
                    <div className="blog-header">
                      <div className="blog-author">
                        {renderUserAvatar(blog.userId, 32)}
                        <div className="author-info">
                          <h4>
                            {blogUser?.fullName || `User ${blog.userId}`}
                            {isMyBlog && (
                              <Tag color="green" size="small">
                                Của bạn
                              </Tag>
                            )}
                          </h4>
                          <div className="blog-meta">
                            <span>
                              <CalendarOutlined /> {formatDateTime(blog.postAt)}
                            </span>
                            {blog.updatedAt &&
                              blog.updatedAt !== blog.postAt && (
                                <span className="updated">
                                  <EditOutlined /> Đã sửa{" "}
                                  {formatDateTime(blog.updatedAt)}
                                </span>
                              )}
                          </div>
                        </div>
                      </div>
                      {renderBlogActions(blog)}
                    </div>

                    {/* Blog Content */}
                    <div className="blog-content">
                      <h3 className="blog-title">{blog.title}</h3>
                      <div className="blog-text">{blog.content}</div>

                      {renderBlogCardImages(blog)}
                    </div>

                    {/* Blog Actions */}
                    <div className="blog-actions">
                      <Button
                        type="text"
                        onClick={() => openCommentModal(blog)}
                        className="comment-btn"
                      >
                        💬{" "}
                        {blogCommentCounts[blog.blogId] ??
                          blog.totalComments ??
                          0}{" "}
                        bình luận
                      </Button>
                    </div>
                  </Card>
                );
              })
            )}
          </Spin>
        </div>

        {/* Ultra Compact Pagination */}
        {totalBlogs > pageSize && (
          <div className="blog-pagination-ultra">
            <Pagination
              current={currentPage}
              pageSize={pageSize}
              total={totalBlogs}
              onChange={(page) => setCurrentPage(page)}
              showSizeChanger={false}
              showQuickJumper={false}
              size="small"
            />
          </div>
        )}
      </div>

      {/* Comment Modal */}
      <CommentModal
        visible={commentModalVisible}
        blog={currentBlogForComments}
        currentUser={currentUser}
        userCache={userCache}
        blogImagesCache={blogImagesCache}
        onClose={closeCommentModal}
        onCommentCountUpdate={handleCommentCountUpdate}
        renderUserAvatar={renderUserAvatar}
        renderBlogImages={renderBlogImages}
        formatDateTime={formatDateTime}
      />

      {/* Blog Create/Edit Modal */}
      <BlogModal
        visible={blogModalVisible}
        editingBlog={editingBlog}
        currentUser={currentUser}
        onClose={closeBlogModal}
        onSuccess={handleBlogSuccess}
        setBlogImagesCache={setBlogImagesCache}
      />
    </div>
  );
};

export default Blog;
