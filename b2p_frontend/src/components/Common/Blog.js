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
  SendOutlined,
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
  getAllComments,
  createComment,
  updateComment,
  deleteComment,
  uploadBlogImage,
  getBlogImages,
  deleteImage,
  getUserById,
} from "../../services/apiService";
import "./Blog.scss";
import styles from "./CommentModal.module.scss";
import signalRService from "../../services/signalRService";

const { TextArea } = Input;
const { Dragger } = Upload;
const { Option } = Select;
const { Search } = Input;

const convertGoogleDriveUrl = (url) => {
  if (!url) return "";

  // Chỉ xử lý link drive.google.com/uc?id=<id>
  const fileIdMatch = url.match(/[?&]id=([a-zA-Z0-9-_]+)/);
  if (fileIdMatch) {
    const fileId = fileIdMatch[1];
    // Trả về link thumbnail (hoặc có thể đổi thành link gốc nếu muốn)
    return `https://drive.google.com/thumbnail?id=${fileId}&sz=w800`;
    // hoặc: return `https://drive.google.com/uc?export=view&id=${fileId}`;
  }

  // Nếu không đúng format thì trả lại url gốc
  return url;
};

const Blog = () => {
  // =============== STATE MANAGEMENT ===============
  const [blogs, setBlogs] = useState([]);
  const [loading, setLoading] = useState(false);

  // 🎯 Add blog images cache state
  const [blogImagesCache, setBlogImagesCache] = useState({});
  const [loadingBlogImages, setLoadingBlogImages] = useState({});

  // Current user info - Updated: 2025-07-26 17:51:24
  const { user, isLoggedIn, isLoading: authLoading } = useAuth();
  const [currentUser] = useState({
    userId: user?.userId,
    fullName: user?.fullName || "",
    userName: user?.userName || "DuyQuan226",
    avatar:
      user?.avatar ||
      "https://ui-avatars.com/api/?name=DuyQuan226&background=27ae60&color=fff&size=200",
    roleId: user?.roleId || 2,
  });

  // User cache for displaying other users
  const [userCache, setUserCache] = useState({
    26: currentUser,
  });

  // Pagination & Search/Sort/Filter
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize] = useState(4); // Reduced from 5 to 4
  const [totalBlogs, setTotalBlogs] = useState(0);
  const [searchTerm, setSearchTerm] = useState("");
  const [sortBy, setSortBy] = useState("postAt");
  const [sortDirection, setSortDirection] = useState("desc");
  const [isSearching, setIsSearching] = useState(false);
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);

  // Blog Modal
  const [blogModalVisible, setBlogModalVisible] = useState(false);
  const [editingBlog, setEditingBlog] = useState(null);
  const [blogForm] = Form.useForm();
  const [blogImages, setBlogImages] = useState([]);
  const [blogImageLoading, setBlogImageLoading] = useState(false);
  const [uploadingImages, setUploadingImages] = useState([]);

  // Comments Modal - Facebook-style with nested replies
  const [commentModalVisible, setCommentModalVisible] = useState(false);
  const [currentBlogForComments, setCurrentBlogForComments] = useState(null);
  const [comments, setComments] = useState([]);
  const [commentInput, setCommentInput] = useState("");
  const [replyInputs, setReplyInputs] = useState({});
  const [editingComment, setEditingComment] = useState(null);
  const [loadingComments, setLoadingComments] = useState(false);
  const [currentBlogImages, setCurrentBlogImages] = useState([]);
  const [showReplyInputs, setShowReplyInputs] = useState({});

  // Nested replies states
  const [nestedReplyInputs, setNestedReplyInputs] = useState({});
  const [showNestedReplyInputs, setShowNestedReplyInputs] = useState({});
  const [blogCommentCounts, setBlogCommentCounts] = useState({});

  // Thêm state cho image modal
  const [imageModalVisible, setImageModalVisible] = useState(false);
  const [selectedImages, setSelectedImages] = useState([]);
  const [currentImageIndex, setCurrentImageIndex] = useState(0);

  // Thêm state để quản lý ảnh pending (chờ upload/delete)
  const [pendingImageUploads, setPendingImageUploads] = useState([]); // Ảnh chờ upload
  const [pendingImageDeletes, setPendingImageDeletes] = useState([]); // Ảnh chờ xóa

  // =============== HELPER FUNCTIONS ===============
  const getCurrentDateTime = () => {
    return new Date().toISOString();
  };

  const formatDateTime = (dateString) => {
    const date = new Date(dateString);
    const now = new Date(); // Lấy thời gian hiện tại thực tế
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

    // Fallback user info
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

  // 🎯 Add function to fetch blog images with caching
  const fetchBlogImagesForCard = async (blogId) => {
    // Return cached images if available
    if (blogImagesCache[blogId]) {
      return blogImagesCache[blogId];
    }

    // Return empty array if already loading
    if (loadingBlogImages[blogId]) {
      return [];
    }

    try {
      setLoadingBlogImages((prev) => ({ ...prev, [blogId]: true }));

      const response = await getBlogImages(blogId);

      // Check if response is already the array (axios interceptor returns response.data)
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

  // Register comment count callbacks for all blogs
  useEffect(() => {
    // ✅ FIX: Store callbacks in ref để có thể cleanup
    const callbacksRef = new Map();

    blogs.forEach((blog) => {
      const updateCommentCount = (increment) => {
        console.log(
          `📊 Updating comment count for blog ${blog.blogId} by ${increment}`
        );

        setBlogCommentCounts((prev) => ({
          ...prev,
          [blog.blogId]: Math.max(
            (prev[blog.blogId] || blog.totalComments || 0) + increment,
            0
          ),
        }));

        setBlogs((prevBlogs) =>
          prevBlogs.map((b) =>
            b.blogId === blog.blogId
              ? {
                  ...b,
                  totalComments: Math.max(
                    (b.totalComments || 0) + increment,
                    0
                  ),
                }
              : b
          )
        );
      };

      // ✅ Store callback để có thể cleanup sau
      callbacksRef.set(blog.blogId, updateCommentCount);

      if (window.registerCommentCountCallback) {
        window.registerCommentCountCallback(blog.blogId, updateCommentCount);
      }
    });

    // Initialize comment counts
    const initialCounts = {};
    blogs.forEach((blog) => {
      initialCounts[blog.blogId] = blog.totalComments || 0;
    });
    setBlogCommentCounts(initialCounts);

    // ✅ FIX: Cleanup sử dụng callbacks từ ref
    return () => {
      callbacksRef.forEach((callback, blogId) => {
        if (window.unregisterCommentCountCallback) {
          window.unregisterCommentCountCallback(blogId, callback);
        }
      });
      callbacksRef.clear();
    };
  }, [blogs]);

  // Debounced search effect
  useEffect(() => {
    const timeoutId = setTimeout(() => {
      if (searchTerm !== "" || isSearching) {
        setCurrentPage(1);
        fetchBlogs();
      }
    }, 500);

    return () => clearTimeout(timeoutId);
  }, [searchTerm]);

  // 🎯 Effect to fetch images for all blogs when blogs change
  useEffect(() => {
    if (blogs.length > 0) {
      blogs.forEach((blog) => {
        fetchBlogImagesForCard(blog.blogId);
      });
    }
  }, [blogs]);

  // =============== BLOG FUNCTIONS WITH SEARCH & SORT ===============
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

  // Handle search
  const handleSearch = (value) => {
    setSearchTerm(value);
  };

  // Handle sort change
  const handleSortChange = (newSortBy) => {
    if (newSortBy === sortBy) {
      setSortDirection((prev) => (prev === "asc" ? "desc" : "asc"));
    } else {
      setSortBy(newSortBy);
      setSortDirection("desc");
    }
    setCurrentPage(1);
  };

  // Clear all filters
  const handleClearFilters = () => {
    setSearchTerm("");
    setSortBy("postAt");
    setSortDirection("desc");
    setCurrentPage(1);
    setIsSearching(false);
  };

  // Toggle sidebar
  const toggleSidebar = () => {
    setSidebarCollapsed(!sidebarCollapsed);
  };

  // =============== COMMENT MODAL FUNCTIONS ===============
  const openCommentModal = async (blog) => {
    // ✅ THÊM: Track modal đang mở để refresh real-time
    window.currentOpenBlogId = blog.blogId;
    window.refreshCommentsCallback = fetchCommentsForBlog;

    // ✅ THÊM: Update với comment count mới nhất
    const updatedBlog = {
      ...blog,
      totalComments: blogCommentCounts[blog.blogId] ?? blog.totalComments ?? 0,
    };

    setCurrentBlogForComments(updatedBlog);
    setCommentModalVisible(true); // ✅ Sửa tên state cho đúng
    setComments([]);
    setCurrentBlogImages([]);
    setCommentInput("");
    setReplyInputs({});
    setEditingComment(null);
    setShowReplyInputs({});
    setNestedReplyInputs({});
    setShowNestedReplyInputs({});

    await Promise.all([
      fetchCommentsForBlog(blog.blogId),
      fetchBlogImagesForModal(blog.blogId),
    ]);
  };

  const closeCommentModal = () => {
    setCommentModalVisible(false);
    setCurrentBlogForComments(null);
    setComments([]);
    setCurrentBlogImages([]);
    setCommentInput("");
    setReplyInputs({});
    setEditingComment(null);
    setShowReplyInputs({});
    setNestedReplyInputs({});
    setShowNestedReplyInputs({});

    // ✅ THÊM: Clear tracking để tắt real-time updates
    window.currentOpenBlogId = null;
    window.refreshCommentsCallback = null;

    console.log("🗑️ Cleared modal tracking - real-time updates disabled");
  };
  useEffect(() => {
    if (currentBlogForComments && currentBlogForComments.blogId) {
      const newCount = blogCommentCounts[currentBlogForComments.blogId];
      if (
        newCount !== undefined &&
        newCount !== currentBlogForComments.totalComments
      ) {
        console.log(
          `📊 Updating modal comment count from ${currentBlogForComments.totalComments} to ${newCount}`
        );
        setCurrentBlogForComments((prev) => ({
          ...prev,
          totalComments: newCount,
        }));
      }
    }
  }, [blogCommentCounts, currentBlogForComments]);
  const fetchCommentsForBlog = async (blogId) => {
    try {
      setLoadingComments(true);

      const response = await getAllComments({
        search: "",
        page: 1,
        pageSize: 100,
        sortBy: "postAt",
        sortDirection: "asc",
      });

      if (response && response.success) {
        const blogComments =
          response.data.items?.filter((comment) => comment.blogId === blogId) ||
          [];

        setComments(blogComments);

        const commentUserIds = [
          ...new Set(blogComments.map((comment) => comment.userId)),
        ];
        commentUserIds.forEach((userId) => {
          if (!userCache[userId]) {
            getUserInfo(userId);
          }
        });
      }
    } catch (error) {
      console.error("Error fetching comments:", error);
      message.error("Không thể tải bình luận");
    } finally {
      setLoadingComments(false);
    }
  };

  // Sửa function fetchBlogImagesForModal
  const fetchBlogImagesForModal = async (blogId) => {
    try {
      const response = await getBlogImages(blogId);

      // 🎯 Fix: Check response structure like in fetchBlogImagesForCard
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

        setCurrentBlogImages(images);
      } else {
        setCurrentBlogImages([]);
      }
    } catch (error) {
      console.error("Error fetching blog images:", error);
      setCurrentBlogImages([]);
    }
  };

  const handleCommentSubmit = async () => {
    if (!commentInput.trim()) {
      message.warning("Vui lòng nhập nội dung bình luận!");
      return;
    }

    if (!currentBlogForComments) return;

    try {
      const commentData = {
        userId: currentUser.userId,
        blogId: currentBlogForComments.blogId,
        content: commentInput.trim(),
        parentCommentId: null,
      };

      const response = await createComment(commentData);
      if (response && response.success) {
        setCommentInput("");

        await fetchCommentsForBlog(currentBlogForComments.blogId);

        // ✅ THAY THẾ ĐOẠN TRÊN BẰNG ĐOẠN NÀY
        const newCount = (currentBlogForComments.totalComments || 0) + 1;

        setBlogCommentCounts((prev) => ({
          ...prev,
          [currentBlogForComments.blogId]: newCount,
        }));

        setBlogs((prevBlogs) =>
          prevBlogs.map((blog) =>
            blog.blogId === currentBlogForComments.blogId
              ? { ...blog, totalComments: newCount }
              : blog
          )
        );

        setCurrentBlogForComments((prev) => ({
          ...prev,
          totalComments: (prev.totalComments || 0) + 1,
        }));

        // ✅ NEW: Send comment notification via SignalR
        if (signalRService.connected) {
          const notification = {
            commentId: response.data?.commentId || Date.now(),
            userId: currentUser.userId,
            userName: currentUser.fullName,
            userAvatar: currentUser.avatar,
            blogId: currentBlogForComments.blogId,
            blogTitle: currentBlogForComments.title,
            blogAuthorId: currentBlogForComments.userId,
            content: commentInput.trim(),
            isReply: false,
            timestamp: new Date().toISOString(),
            action: "comment_created",
          };

          try {
            await signalRService.sendCommentNotification(notification);
            console.log("📤 Comment notification sent:", notification);
          } catch (error) {
            console.error("❌ Error sending comment notification:", error);
          }
        }

        message.success("💬 Đã thêm bình luận!");
      } else {
        message.error(response?.message || "Không thể thêm bình luận");
      }
    } catch (error) {
      console.error("Error creating comment:", error);
      message.error("Có lỗi khi thêm bình luận");
    }
  };

  const handleReplySubmit = async (parentCommentId) => {
    const content = replyInputs[parentCommentId];
    if (!content?.trim()) {
      message.warning("Vui lòng nhập nội dung trả lời!");
      return;
    }

    if (!currentBlogForComments) return;

    try {
      const replyData = {
        userId: currentUser.userId,
        blogId: currentBlogForComments.blogId,
        content: content.trim(),
        parentCommentId: parentCommentId,
      };

      const response = await createComment(replyData);
      if (response && response.success) {
        setReplyInputs((prev) => ({ ...prev, [parentCommentId]: "" }));
        setShowReplyInputs((prev) => ({ ...prev, [parentCommentId]: false }));

        await fetchCommentsForBlog(currentBlogForComments.blogId);

        setBlogs((prevBlogs) =>
          prevBlogs.map((blog) =>
            blog.blogId === currentBlogForComments.blogId
              ? { ...blog, totalComments: (blog.totalComments || 0) + 1 }
              : blog
          )
        );

        setCurrentBlogForComments((prev) => ({
          ...prev,
          totalComments: (prev.totalComments || 0) + 1,
        }));

        // ✅ NEW: Send reply notification via SignalR
        if (signalRService.connected) {
          const parentComment = comments.find(
            (c) => c.commentId === parentCommentId
          );

          const notification = {
            commentId: response.data?.commentId || Date.now(),
            userId: currentUser.userId,
            userName: currentUser.fullName,
            userAvatar: currentUser.avatar,
            blogId: currentBlogForComments.blogId,
            blogTitle: currentBlogForComments.title,
            blogAuthorId: currentBlogForComments.userId,
            content: content.trim(),
            isReply: true,
            parentCommentId: parentCommentId,
            parentComment: parentComment?.content?.substring(0, 50) + "...",
            timestamp: new Date().toISOString(),
            action: "comment_reply",
          };

          try {
            await signalRService.sendCommentNotification(notification);
            console.log("📤 Reply notification sent:", notification);
          } catch (error) {
            console.error("❌ Error sending reply notification:", error);
          }
        }

        message.success("↩️ Đã trả lời bình luận!");
      } else {
        message.error(response?.message || "Không thể trả lời bình luận");
      }
    } catch (error) {
      console.error("Error creating reply:", error);
      message.error("Có lỗi khi trả lời bình luận");
    }
  };

  const handleNestedReplySubmit = async (parentReplyId) => {
    const content = nestedReplyInputs[parentReplyId];
    if (!content?.trim()) {
      message.warning("Vui lòng nhập nội dung trả lời!");
      return;
    }

    if (!currentBlogForComments) return;

    try {
      const replyData = {
        userId: currentUser.userId,
        blogId: currentBlogForComments.blogId,
        content: content.trim(),
        parentCommentId: parentReplyId,
      };

      const response = await createComment(replyData);
      if (response && response.success) {
        setNestedReplyInputs((prev) => ({ ...prev, [parentReplyId]: "" }));
        setShowNestedReplyInputs((prev) => ({
          ...prev,
          [parentReplyId]: false,
        }));

        await fetchCommentsForBlog(currentBlogForComments.blogId);

        setBlogs((prevBlogs) =>
          prevBlogs.map((blog) =>
            blog.blogId === currentBlogForComments.blogId
              ? { ...blog, totalComments: (blog.totalComments || 0) + 1 }
              : blog
          )
        );

        setCurrentBlogForComments((prev) => ({
          ...prev,
          totalComments: (prev.totalComments || 0) + 1,
        }));

        // ✅ NEW: Send nested reply notification via SignalR
        if (signalRService.connected) {
          const parentReply = comments.find(
            (c) => c.commentId === parentReplyId
          );

          const notification = {
            commentId: response.data?.commentId || Date.now(),
            userId: currentUser.userId,
            userName: currentUser.fullName,
            userAvatar: currentUser.avatar,
            blogId: currentBlogForComments.blogId,
            blogTitle: currentBlogForComments.title,
            blogAuthorId: currentBlogForComments.userId,
            content: content.trim(),
            isReply: true,
            parentCommentId: parentReplyId,
            parentComment: parentReply?.content?.substring(0, 50) + "...",
            timestamp: new Date().toISOString(),
            action: "nested_reply",
          };

          try {
            await signalRService.sendCommentNotification(notification);
            console.log("📤 Nested reply notification sent:", notification);
          } catch (error) {
            console.error("❌ Error sending nested reply notification:", error);
          }
        }

        message.success("↩️ Đã trả lời bình luận!");
      } else {
        message.error(response?.message || "Không thể trả lời bình luận");
      }
    } catch (error) {
      console.error("Error creating nested reply:", error);
      message.error("Có lỗi khi trả lời bình luận");
    }
  };

  const handleCommentEdit = async (commentId, content) => {
    if (!content?.trim()) {
      message.warning("Nội dung bình luận không được để trống!");
      return;
    }

    if (!currentBlogForComments) return;

    try {
      const commentData = {
        userId: currentUser.userId,
        blogId: currentBlogForComments.blogId,
        content: content.trim(),
        parentCommentId: null,
      };

      const response = await updateComment(commentId, commentData);
      if (response && response.success) {
        setEditingComment(null);
        await fetchCommentsForBlog(currentBlogForComments.blogId);
        message.success("✏️ Cập nhật bình luận thành công!");
      } else {
        message.error(response?.message || "Không thể cập nhật bình luận");
      }
    } catch (error) {
      console.error("Error updating comment:", error);
      message.error("Có lỗi khi cập nhật bình luận");
    }
  };

  const handleCommentDelete = (comment) => {
    Modal.confirm({
      title: "🗑️ Xác nhận xóa bình luận",
      content: (
        <div>
          <p>Bạn có chắc chắn muốn xóa bình luận này không?</p>
          <div
            style={{
              background: "#f8f9fa",
              padding: "12px",
              borderRadius: "8px",
              marginTop: "12px",
            }}
          >
            <em>"{comment.content}"</em>
          </div>
        </div>
      ),
      okText: "Xóa",
      cancelText: "Hủy",
      okType: "danger",
      onOk: async () => {
        try {
          const response = await deleteComment(
            comment.commentId,
            currentUser.userId,
            currentUser.roleId
          );
          if (response && response.success) {
            await fetchCommentsForBlog(currentBlogForComments.blogId);

            setBlogs((prevBlogs) =>
              prevBlogs.map((blog) =>
                blog.blogId === currentBlogForComments.blogId
                  ? {
                      ...blog,
                      totalComments: Math.max((blog.totalComments || 0) - 1, 0),
                    }
                  : blog
              )
            );

            setCurrentBlogForComments((prev) => ({
              ...prev,
              totalComments: Math.max((prev.totalComments || 0) - 1, 0),
            }));

            message.success("🗑️ Xóa bình luận thành công!");
          } else {
            message.error(response?.message || "Không thể xóa bình luận");
          }
        } catch (error) {
          console.error("Error deleting comment:", error);
          message.error("Có lỗi khi xóa bình luận");
        }
      },
    });
  };

  // Toggle functions
  const toggleReplyInput = (commentId) => {
    setShowReplyInputs((prev) => ({
      ...prev,
      [commentId]: !prev[commentId],
    }));
  };

  const toggleNestedReplyInput = (replyId) => {
    setShowNestedReplyInputs((prev) => ({
      ...prev,
      [replyId]: !prev[replyId],
    }));
  };

  // =============== BLOG FUNCTIONS ===============
  const openBlogModal = (blog = null) => {
    setEditingBlog(blog);
    setBlogImages([]);
    setUploadingImages([]);

    // 🎯 Reset pending changes
    setPendingImageUploads([]);
    setPendingImageDeletes([]);

    if (blog) {
      blogForm.setFieldsValue({
        title: blog.title,
        content: blog.content,
      });
      fetchBlogImages(blog.blogId);
    } else {
      blogForm.resetFields();
    }

    setBlogModalVisible(true);
  };

  // Sửa lại function fetchBlogImages để handle response structure đúng
  const fetchBlogImages = async (blogId) => {
    try {
      setBlogImageLoading(true);
      const response = await getBlogImages(blogId);

      // 🎯 Fix: Handle response structure like other functions
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

        setBlogImages(images);
      } else {
        setBlogImages([]);
      }
    } catch (error) {
      console.error("Error fetching blog images:", error);
      setBlogImages([]);
    } finally {
      setBlogImageLoading(false);
    }
  };

  // Sửa lại function handleImageUpload - loại bỏ thông báo success
  const handleImageUpload = async (file, blogId = null) => {
    const isJpgOrPng =
      file.type === "image/jpeg" ||
      file.type === "image/png" ||
      file.type === "image/gif";
    if (!isJpgOrPng) {
      message.error("Chỉ có thể upload file JPG/PNG/GIF!");
      return false;
    }

    const isLt10M = file.size / 1024 / 1024 < 10;
    if (!isLt10M) {
      message.error("Ảnh phải nhỏ hơn 10MB!");
      return false;
    }

    if (blogId) {
      // Editing mode - add to pending list
      const tempId = `temp_${Date.now()}_${Math.random()}`;
      const newPendingImage = {
        tempId: tempId,
        file: file,
        name: file.name,
        url: URL.createObjectURL(file),
        isNew: true,
      };

      setPendingImageUploads((prev) => [...prev, newPendingImage]);

      const previewImage = {
        imageId: tempId,
        imageUrl: URL.createObjectURL(file),
        caption: "Ảnh mới (chưa lưu)",
        isPreview: true,
      };

      setBlogImages((prevImages) => [...prevImages, previewImage]);
      // 🎯 No success message here
      return false;
    } else {
      // Creating new blog
      setUploadingImages((prev) => [
        ...prev,
        {
          uid: file.uid,
          file: file,
          name: file.name,
          status: "done",
          url: URL.createObjectURL(file),
        },
      ]);
      return false;
    }
  };

  // Sửa lại function handleImageDelete - loại bỏ thông báo success
  const handleImageDelete = async (imageId, blogId) => {
    if (typeof imageId === "string" && imageId.startsWith("temp_")) {
      setPendingImageUploads((prev) =>
        prev.filter((img) => img.tempId !== imageId)
      );

      setBlogImages((prevImages) =>
        prevImages.filter((img) => img.imageId !== imageId)
      );
      // 🎯 No success message here
      return;
    }

    if (!pendingImageDeletes.includes(imageId)) {
      setPendingImageDeletes((prev) => [...prev, imageId]);
    }

    setBlogImages((prevImages) =>
      prevImages.filter((img) => img.imageId !== imageId)
    );
    // 🎯 No success message here
  };

  const handleRemoveUploadingImage = (uid) => {
    setUploadingImages((prev) => prev.filter((img) => img.uid !== uid));
  };

  // Sửa lại function handleBlogSubmit - loại bỏ thông báo info
  const handleBlogSubmit = async () => {
    try {
      const values = await blogForm.validateFields();
      setLoading(true);

      const blogData = {
        userId: currentUser.userId,
        title: values.title.trim(),
        content: values.content.trim(),
      };

      let response;
      let createdBlogId = null;

      if (editingBlog) {
        const hasBlogContentChanges =
          blogData.title !== editingBlog.title ||
          blogData.content !== editingBlog.content;

        const hasImageChanges =
          pendingImageUploads.length > 0 || pendingImageDeletes.length > 0;

        createdBlogId = editingBlog.blogId;

        // Only call updateBlog API if there are actual blog content changes
        if (hasBlogContentChanges) {
          response = await updateBlog(editingBlog.blogId, blogData);
          if (!response || !response.success) {
            message.error(response?.message || "Không thể cập nhật bài viết");
            return;
          }
        } else {
          response = { success: true };
        }

        if (response && response.success) {
          let imageChangeSuccess = true;

          // 1. Delete marked images - NO info message
          if (pendingImageDeletes.length > 0) {
            for (const imageId of pendingImageDeletes) {
              try {
                const deleteResponse = await deleteImage(imageId);
                if (!deleteResponse || !deleteResponse.success) {
                  imageChangeSuccess = false;
                }
              } catch (error) {
                console.error(`Failed to delete image ${imageId}:`, error);
                imageChangeSuccess = false;
              }
            }
          }

          // 2. Upload new images - NO info message
          if (pendingImageUploads.length > 0) {
            for (const imgData of pendingImageUploads) {
              try {
                const uploadResponse = await uploadBlogImage(
                  imgData.file,
                  createdBlogId,
                  "Blog image"
                );
                if (!uploadResponse || !uploadResponse.success) {
                  imageChangeSuccess = false;
                }
              } catch (error) {
                console.error(`Failed to upload image ${imgData.name}:`, error);
                imageChangeSuccess = false;
              }
            }
          }

          // Clear cache
          setBlogImagesCache((prev) => {
            const newCache = { ...prev };
            delete newCache[createdBlogId];
            return newCache;
          });

          // 🎯 Only show final result message
          if (hasImageChanges && imageChangeSuccess) {
            message.success("✅ Cập nhật thành công!");
          } else if (hasBlogContentChanges) {
            message.success("✅ Cập nhật thành công!");
          }
        }
      } else {
        // Creating new blog
        response = await createBlog(blogData);
        if (response && response.success && response.data) {
          createdBlogId = response.data.blogId || response.data.id;

          // Upload images for new blog - NO info messages
          if (uploadingImages.length > 0 && createdBlogId) {
            for (const imgData of uploadingImages) {
              try {
                await uploadBlogImage(
                  imgData.file,
                  createdBlogId,
                  "Blog image"
                );
              } catch (error) {
                console.error("Error uploading image:", error);
              }
            }
          }

          message.success("🎉 Đăng bài viết thành công!");
        } else {
          message.error(response?.message || "Có lỗi xảy ra khi tạo bài viết");
          return;
        }
      }

      // Reset states
      setPendingImageUploads([]);
      setPendingImageDeletes([]);
      setBlogModalVisible(false);
      fetchBlogs();
    } catch (error) {
      console.error("Error submitting blog:", error);
      if (error.errorFields) {
        message.error("Vui lòng kiểm tra lại thông tin!");
      } else {
        message.error("Có lỗi xảy ra khi lưu bài viết");
      }
    } finally {
      setLoading(false);
    }
  };

  // Sửa lại function handleDeleteBlog - loại bỏ thông báo info
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
          // Get and delete images - NO info messages
          const blogImages = await fetchBlogImagesForCard(blog.blogId);

          if (blogImages && blogImages.length > 0) {
            for (const image of blogImages) {
              try {
                await deleteImage(image.imageId);
              } catch (error) {
                console.error(`Error deleting image ${image.imageId}:`, error);
                // Continue anyway
              }
            }
          }

          // Delete blog - NO info message
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
      // Đã xóa mục báo cáo ở đây
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

  // 🎯 Enhanced function to render blog images
  const renderBlogImages = (images, isInModal = false) => {
    if (!images || images.length === 0) {
      return null;
    }

    // Nếu đang trong modal comment, hiển thị tất cả ảnh
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

    // Trong blog card, hiển thị tối đa 3 ảnh với logic +N
    const maxDisplayImages = 3;
    const displayImages = images.slice(0, maxDisplayImages);
    const remainingCount = images.length - maxDisplayImages;

    // Determine grid class based on image count
    let gridClass = "";
    if (images.length === 1) {
      gridClass = "single-image";
    } else if (images.length === 2) {
      gridClass = "two-images";
    } else if (images.length >= 3) {
      gridClass = "three-or-more-images";
    }

    return (
      <div className="blog-images-display card-images">
        <div className={`image-grid ${gridClass}`}>
          {displayImages.map((image, index) => {
            const convertedUrl = convertGoogleDriveUrl(image.imageUrl);
            const isThirdImage = index === 2; // Ảnh thứ 3 (index 2)
            const hasMoreImages = remainingCount > 0 && isThirdImage;

            return (
              <div
                key={image.imageId || index}
                className={`blog-image-item ${
                  hasMoreImages ? "has-overlay" : ""
                }`}
                style={{ position: "relative" }}
              >
                <Image
                  src={convertedUrl}
                  alt={image.caption || `Blog image ${index + 1}`}
                  style={{
                    borderRadius: 8,
                    width: "100%",
                    height: images.length === 1 ? "300px" : "200px",
                    objectFit: "cover",
                  }}
                  preview={false}
                  fallback="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="
                />
                {/* 🎯 Overlay for +N more images on third image */}
                {hasMoreImages && (
                  <div
                    className="image-overlay-on-third"
                    onClick={(e) => {
                      e.stopPropagation(); // Prevent event bubbling
                      // Tìm blog object từ blogImagesCache
                      const blogEntry = Object.entries(blogImagesCache).find(
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
                    </div>
                  </div>
                )}
              </div>
            );
          })}
        </div>
      </div>
    );
  };

  // Sửa lại renderBlogCardImages để truyền thêm blog info
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

    // 🎯 Wrap với div có onClick để mở CommentModal khi click vào overlay
    return (
      <div
        onClick={(e) => {
          // Chỉ mở modal khi click vào overlay, không phải ảnh thường
          if (e.target.closest(".image-overlay")) {
            openCommentModal(blog);
          }
        }}
        style={{ cursor: "pointer" }}
      >
        {renderBlogImages(images, false)}
      </div>
    );
  };

  // Thêm function render Image Modal
  const renderImageModal = () => {
    if (!selectedImages.length) return null;

    const currentImage = selectedImages[currentImageIndex];
    const convertedUrl = convertGoogleDriveUrl(currentImage.imageUrl);

    const goToPrevious = () => {
      setCurrentImageIndex((prev) =>
        prev > 0 ? prev - 1 : selectedImages.length - 1
      );
    };

    const goToNext = () => {
      setCurrentImageIndex((prev) =>
        prev < selectedImages.length - 1 ? prev + 1 : 0
      );
    };

    return (
      <Modal
        open={imageModalVisible}
        onCancel={() => setImageModalVisible(false)}
        footer={null}
        width="90vw"
        style={{ top: 20 }}
        className="image-viewer-modal"
        closable={false}
        maskClosable={true}
      >
        <div className="image-viewer-container">
          {/* Header */}
          <div className="image-viewer-header">
            <div className="image-counter">
              {currentImageIndex + 1} / {selectedImages.length}
            </div>
            <Button
              type="text"
              icon={<CloseOutlined />}
              onClick={() => setImageModalVisible(false)}
              className="close-btn"
            />
          </div>

          {/* Main Image */}
          <div className="image-viewer-main">
            {selectedImages.length > 1 && (
              <Button
                type="text"
                icon={<LeftOutlined />}
                onClick={goToPrevious}
                className="nav-btn prev-btn"
              />
            )}

            <div className="main-image-container">
              <Image
                src={convertedUrl}
                alt={currentImage.caption || `Image ${currentImageIndex + 1}`}
                style={{
                  maxWidth: "100%",
                  maxHeight: "70vh",
                  objectFit: "contain",
                }}
                preview={false}
              />
              {currentImage.caption && (
                <div className="image-caption">{currentImage.caption}</div>
              )}
            </div>

            {selectedImages.length > 1 && (
              <Button
                type="text"
                icon={<RightOutlined />}
                onClick={goToNext}
                className="nav-btn next-btn"
              />
            )}
          </div>

          {/* Thumbnails */}
          {selectedImages.length > 1 && (
            <div className="image-thumbnails">
              {selectedImages.map((image, index) => (
                <div
                  key={image.imageId || index}
                  className={`thumbnail ${
                    index === currentImageIndex ? "active" : ""
                  }`}
                  onClick={() => setCurrentImageIndex(index)}
                >
                  <Image
                    src={convertGoogleDriveUrl(image.imageUrl)}
                    alt={`Thumbnail ${index + 1}`}
                    style={{
                      width: "60px",
                      height: "60px",
                      objectFit: "cover",
                      borderRadius: "4px",
                    }}
                    preview={false}
                  />
                </div>
              ))}
            </div>
          )}
        </div>
      </Modal>
    );
  };

  // =============== RENDER SIDEBAR ===============
  const renderSidebar = () => (
    <div className={`fixed-sidebar ${sidebarCollapsed ? "collapsed" : ""}`}>
      <Card className="sidebar-card">
        {/* Sidebar Header */}
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

        {/* 🎯 Wrap scrollable content */}
        {!sidebarCollapsed && (
          <div className="sidebar-content">
            {/* Search Section */}
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

            {/* Sort Section */}
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

            {/* Active Filters */}
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

            {/* Search Results */}
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

            {/* Quick Stats */}
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

  // =============== COMMENT MODAL RENDER ===============
  const renderCommentModal = () => {
    if (!currentBlogForComments) return null;

    const blogUser = userCache[currentBlogForComments.userId];
    const mainComments = comments.filter((comment) => !comment.parentCommentId);

    return (
      <Modal
        title={null}
        open={commentModalVisible}
        onCancel={closeCommentModal}
        footer={null}
        width={700}
        className={styles.modal}
        closable={false}
        getContainer={() => document.body}
        maskClosable={true}
        destroyOnClose={true}
        centered={false}
        style={{
          top: "80px",
          zIndex: 10000,
        }}
        zIndex={10000}
      >
        <div className={styles.modalContent}>
          {/* Header */}
          <div className={styles.header}>
            <div className={styles.headerTitle}>
              <h3>
                Bài viết của{" "}
                {blogUser?.fullName || `User ${currentBlogForComments.userId}`}
              </h3>
              <div className={styles.subtitle}>
                📅 26/07/2025 • 🕐 17:17 UTC
              </div>
            </div>
            <button onClick={closeCommentModal} className={styles.closeButton}>
              <CloseOutlined />
            </button>
          </div>

          {/* Blog Detail */}
          <div className={styles.blogDetail}>
            <div className={styles.authorInfo}>
              {renderUserAvatar(currentBlogForComments.userId, 50)}
              <div className={styles.authorDetails}>
                <h4>
                  {blogUser?.fullName ||
                    `User ${currentBlogForComments.userId}`}
                  {currentBlogForComments.userId === 26 && (
                    <Tag size="small" color="green" style={{ marginLeft: 8 }}>
                      🟢 Đang hoạt động
                    </Tag>
                  )}
                </h4>
                <div className={styles.postMeta}>
                  <span>
                    <CalendarOutlined />{" "}
                    {formatDateTime(currentBlogForComments.postAt)}
                  </span>
                  {currentBlogForComments.updatedAt &&
                    currentBlogForComments.updatedAt !==
                      currentBlogForComments.postAt && (
                      <span className={styles.updated}>
                        <EditOutlined /> Đã chỉnh sửa{" "}
                        {formatDateTime(currentBlogForComments.updatedAt)}
                      </span>
                    )}
                </div>
              </div>
            </div>

            <div className={styles.postContent}>
              <h3 className={styles.postTitle}>
                {currentBlogForComments.title}
              </h3>
              <div className={styles.postText}>
                {currentBlogForComments.content}
              </div>
              {/* 🎯 Fix: Pass true for isInModal parameter */}
              {renderBlogImages(currentBlogImages, true)}
            </div>

            <div className={styles.postStats}>
              <Tag icon={<MessageOutlined />}>
                {blogCommentCounts[currentBlogForComments?.blogId] ??
                  currentBlogForComments?.totalComments ??
                  0}{" "}
                bình luận
              </Tag>
            </div>
          </div>

          {/* Comments Section */}
          <div className={styles.commentsSection}>
            <div className={styles.commentsHeader}>
              <h4>
                💬 Bình luận (
                {blogCommentCounts[currentBlogForComments?.blogId] ??
                  currentBlogForComments?.totalComments ??
                  0}
                )
              </h4>
            </div>

            {loadingComments ? (
              <div className={styles.loading}>
                <Spin size="small" />
                <span style={{ marginLeft: 8 }}>Đang tải bình luận...</span>
              </div>
            ) : (
              <div className={styles.commentsList}>
                {mainComments.length === 0 ? (
                  <div className={styles.noComments}>
                    <div className={styles.icon}>
                      <MessageOutlined />
                    </div>
                    <p>Chưa có bình luận nào.</p>
                    <p className={styles.subtitle}>
                      Hãy là người đầu tiên bình luận! {user?.fullName || "Bạn"}{" "}
                      có thể viết ngay ⬇️
                    </p>
                  </div>
                ) : (
                  mainComments.map((comment) => {
                    const commentUser = userCache[comment.userId];
                    const canEditComment =
                      comment.userId === currentUser.userId ||
                      currentUser.roleId === 1;
                    const showReply = showReplyInputs[comment.commentId];

                    return (
                      <div
                        key={comment.commentId}
                        className={styles.commentItem}
                      >
                        <div className={styles.commentHeader}>
                          {renderUserAvatar(comment.userId, "small")}
                          <div className={styles.commentInfo}>
                            <div className={styles.commentAuthor}>
                              {commentUser?.fullName ||
                                `User ${comment.userId}`}
                              {comment.userId === currentUser.userId && (
                                <Tag size="small" color="green">
                                  Bạn
                                </Tag>
                              )}
                            </div>
                            <div className={styles.commentTime}>
                              <ClockCircleOutlined style={{ marginRight: 4 }} />
                              {formatDateTime(comment.postAt)}
                              {comment.updatedAt &&
                                comment.updatedAt !== comment.postAt && (
                                  <span className={styles.edited}>
                                    {" "}
                                    · đã chỉnh sửa
                                  </span>
                                )}
                            </div>
                          </div>

                          {canEditComment && (
                            <Dropdown
                              menu={{
                                items: [
                                  {
                                    key: "edit",
                                    label: "Chỉnh sửa",
                                    icon: <EditOutlined />,
                                    onClick: () =>
                                      setEditingComment(comment.commentId),
                                  },
                                  {
                                    key: "delete",
                                    label: "Xóa",
                                    icon: <DeleteOutlined />,
                                    danger: true,
                                    onClick: () => handleCommentDelete(comment),
                                  },
                                ],
                              }}
                              trigger={["click"]}
                              getPopupContainer={() => document.body}
                            >
                              <button className={styles.commentMenu}>
                                <MoreOutlined />
                              </button>
                            </Dropdown>
                          )}
                        </div>

                        {/* Comment Content */}
                        {editingComment === comment.commentId ? (
                          <div className={styles.editComment}>
                            <TextArea
                              defaultValue={comment.content}
                              autoSize={{ minRows: 2, maxRows: 4 }}
                              autoFocus
                            />
                            <div className={styles.editActions}>
                              <Button
                                size="small"
                                onClick={() => setEditingComment(null)}
                              >
                                Hủy
                              </Button>
                              <Button
                                type="primary"
                                size="small"
                                onClick={() => {
                                  const textarea = document.querySelector(
                                    `.${styles.editComment} textarea`
                                  );
                                  handleCommentEdit(
                                    comment.commentId,
                                    textarea.value
                                  );
                                }}
                              >
                                Lưu
                              </Button>
                            </div>
                          </div>
                        ) : (
                          <div className={styles.commentBubble}>
                            <p>{comment.content}</p>
                          </div>
                        )}

                        {/* Comment Actions */}
                        <div className={styles.commentActions}>
                          <button
                            className={styles.replyButton}
                            onClick={() => toggleReplyInput(comment.commentId)}
                          >
                            {showReply ? "Ẩn trả lời" : "Trả lời"}
                          </button>
                        </div>

                        {/* Reply Input */}
                        <div
                          className={`${styles.replyInput} ${
                            showReply ? styles.show : ""
                          }`}
                        >
                          {renderUserAvatar(currentUser.userId, 24)}
                          <Input
                            placeholder={`Trả lời ${
                              commentUser?.fullName || "bình luận này"
                            }...`}
                            value={replyInputs[comment.commentId] || ""}
                            onChange={(e) =>
                              setReplyInputs((prev) => ({
                                ...prev,
                                [comment.commentId]: e.target.value,
                              }))
                            }
                            onPressEnter={() =>
                              handleReplySubmit(comment.commentId)
                            }
                            suffix={
                              <button
                                className={styles.sendButton}
                                onClick={() =>
                                  handleReplySubmit(comment.commentId)
                                }
                                disabled={
                                  !replyInputs[comment.commentId]?.trim()
                                }
                              >
                                <SendOutlined />
                              </button>
                            }
                          />
                        </div>

                        {/* Nested Replies Container */}
                        <div className={styles.repliesContainer}>
                          {renderNestedReplies(comment.commentId)}
                        </div>
                      </div>
                    );
                  })
                )}
              </div>
            )}

            {/* Main Comment Input */}
            <div className={styles.mainInput}>
              <div className={styles.inputContainer}>
                {renderUserAvatar(currentUser.userId, "default")}
                <div className={styles.inputWrapper}>
                  <TextArea
                    placeholder={`Viết bình luận với tư cách DuyQuan226...`}
                    value={commentInput}
                    onChange={(e) => setCommentInput(e.target.value)}
                    autoSize={{ minRows: 1, maxRows: 4 }}
                    onPressEnter={(e) => {
                      if (e.key === "Enter" && !e.shiftKey) {
                        e.preventDefault();
                        handleCommentSubmit();
                      }
                    }}
                  />
                  <div className={styles.inputActions}>
                    <button
                      className={styles.submitButton}
                      onClick={handleCommentSubmit}
                      disabled={!commentInput.trim()}
                    >
                      <SendOutlined />
                      Đăng
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </Modal>
    );
  };

  // =============== RENDER NESTED REPLIES ===============
  const renderNestedReplies = (parentCommentId) => {
    const replies = comments.filter(
      (comment) => comment.parentCommentId === parentCommentId
    );

    if (replies.length === 0) return null;

    return replies.map((reply) => {
      const replyUser = userCache[reply.userId];
      const canEditReply =
        reply.userId === currentUser.userId || currentUser.roleId === 1;
      const showNestedReply = showNestedReplyInputs[reply.commentId];

      return (
        <div key={reply.commentId} className={styles.replyItem}>
          <div className={styles.replyHeader}>
            {renderUserAvatar(reply.userId, "small")}
            <div className={styles.replyInfo}>
              <div className={styles.replyAuthor}>
                {replyUser?.fullName || `User ${reply.userId}`}
                {reply.userId === currentUser.userId && (
                  <Tag size="small" color="green">
                    Bạn
                  </Tag>
                )}
              </div>
              <div className={styles.replyTime}>
                <ClockCircleOutlined style={{ marginRight: 4 }} />
                {formatDateTime(reply.postAt)}
                {reply.updatedAt && reply.updatedAt !== reply.postAt && (
                  <span className={styles.edited}> · đã chỉnh sửa</span>
                )}
              </div>
            </div>

            {canEditReply && (
              <Dropdown
                menu={{
                  items: [
                    {
                      key: "edit",
                      label: "Chỉnh sửa",
                      icon: <EditOutlined />,
                      onClick: () => setEditingComment(reply.commentId),
                    },
                    {
                      key: "delete",
                      label: "Xóa",
                      icon: <DeleteOutlined />,
                      danger: true,
                      onClick: () => handleCommentDelete(reply),
                    },
                  ],
                }}
                trigger={["click"]}
                getPopupContainer={() => document.body}
              >
                <button className={styles.commentMenu}>
                  <MoreOutlined />
                </button>
              </Dropdown>
            )}
          </div>

          {/* Reply Content */}
          {editingComment === reply.commentId ? (
            <div className={styles.editComment}>
              <TextArea
                defaultValue={reply.content}
                autoSize={{ minRows: 2, maxRows: 4 }}
                autoFocus
              />
              <div className={styles.editActions}>
                <Button size="small" onClick={() => setEditingComment(null)}>
                  Hủy
                </Button>
                <Button
                  type="primary"
                  size="small"
                  onClick={() => {
                    const textarea = document.querySelector(
                      `.${styles.editComment} textarea`
                    );
                    handleCommentEdit(reply.commentId, textarea.value);
                  }}
                >
                  Lưu
                </Button>
              </div>
            </div>
          ) : (
            <div className={styles.replyBubble}>
              <p>{reply.content}</p>
            </div>
          )}

          {/* Nested Reply Actions */}
          <div className={styles.replyActions}>
            <button
              className={styles.replyButton}
              onClick={() => toggleNestedReplyInput(reply.commentId)}
            >
              {showNestedReply ? "Ẩn trả lời" : "Trả lời"}
            </button>
          </div>

          {/* Nested Reply Input */}
          <div
            className={`${styles.nestedReplyInput} ${
              showNestedReply ? styles.show : ""
            }`}
          >
            {renderUserAvatar(currentUser.userId, 20)}
            <Input
              placeholder={`Trả lời ${
                replyUser?.fullName || "bình luận này"
              }...`}
              value={nestedReplyInputs[reply.commentId] || ""}
              onChange={(e) =>
                setNestedReplyInputs((prev) => ({
                  ...prev,
                  [reply.commentId]: e.target.value,
                }))
              }
              onPressEnter={() => handleNestedReplySubmit(reply.commentId)}
              suffix={
                <button
                  className={styles.sendButton}
                  onClick={() => handleNestedReplySubmit(reply.commentId)}
                  disabled={!nestedReplyInputs[reply.commentId]?.trim()}
                >
                  <SendOutlined />
                </button>
              }
            />
          </div>

          {/* Further nested replies */}
          <div className={styles.nestedRepliesContainer}>
            {renderNestedReplies(reply.commentId)}
          </div>
        </div>
      );
    });
  };

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

                      {/* Blog images - Pass blog object instead of just blogId */}
                      {renderBlogCardImages(blog)}
                    </div>

                    {/* Blog Actions */}
                    <div className="blog-actions">
                      <Button
                        type="text"
                        icon={<MessageOutlined />}
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
              // showTotal={(total, range) => `${range[0]}-${range[1]}/${total}`}
              size="small"
            />
          </div>
        )}
      </div>

      {/* Comment Modal */}
      {renderCommentModal()}

      {/* Image Viewer Modal */}
      {renderImageModal()}

      {/* Blog Create/Edit Modal */}
      <Modal
        title={
          <div style={{ display: "flex", alignItems: "center", gap: "12px" }}>
            {renderUserAvatar(currentUser.userId, 40)}

            <div>
              <div style={{ fontWeight: 600 }}>
                {editingBlog ? "✏️ Chỉnh sửa bài viết" : "📝 Tạo bài viết mới"}
              </div>
              <div
                style={{
                  fontSize: "14px",
                  color: "#6c757d",
                  fontWeight: "normal",
                }}
              >
                Đăng với tư cách {currentUser.fullName}
              </div>
            </div>
          </div>
        }
        open={blogModalVisible}
        onOk={handleBlogSubmit}
        onCancel={() => setBlogModalVisible(false)}
        okText={editingBlog ? "💾 Cập nhật" : "🚀 Đăng bài"}
        cancelText="❌ Hủy"
        width={700}
        confirmLoading={loading}
        className="blog-modal"
        okButtonProps={{
          size: "large",
          style: {
            borderRadius: "8px",
            fontWeight: "600",
            background: "linear-gradient(135deg, #27ae60, #2ecc71)",
            border: "none",
          },
        }}
        cancelButtonProps={{
          size: "large",
          style: { borderRadius: "8px", fontWeight: "600" },
        }}
      >
        <Form form={blogForm} layout="vertical">
          <Form.Item
            label="📋 Tiêu đề bài viết"
            name="title"
            rules={[
              { required: true, message: "⚠️ Vui lòng nhập tiêu đề!" },
              { min: 5, message: "📝 Tiêu đề phải có ít nhất 5 ký tự!" },
              {
                max: 100,
                message: "📏 Tiêu đề không được vượt quá 100 ký tự!",
              },
            ]}
          >
            <Input
              placeholder="Nhập tiêu đề bài viết..."
              style={{ borderRadius: "8px" }}
              showCount
              maxLength={100}
            />
          </Form.Item>
          <Form.Item
            label="📄 Nội dung bài viết"
            name="content"
            rules={[
              { required: true, message: "⚠️ Vui lòng nhập nội dung!" },
              { min: 10, message: "📝 Nội dung phải có ít nhất 10 ký tự!" },
              {
                max: 300,
                message: "📏 Nội dung không được vượt quá 300 ký tự",
              },
            ]}
          >
            <TextArea
              placeholder={`Chào ${currentUser.fullName}! Hãy chia sẻ suy nghĩ của bạn về thể thao, kinh nghiệm tập luyện, hoặc những khoảnh khắc đáng nhớ...`}
              autoSize={{ minRows: 4, maxRows: 8 }}
              showCount
              maxLength={300}
              style={{ borderRadius: "8px" }}
            />
          </Form.Item>
          <Form.Item label="🖼️ Hình ảnh đính kèm">
            {editingBlog ? (
              <div>
                <Spin spinning={blogImageLoading} tip="Đang tải ảnh...">
                  {blogImages.length > 0 && (
                    <div
                      className="blog-images"
                      style={{ marginBottom: "16px" }}
                    >
                      <h4
                        style={{
                          marginBottom: "12px",
                          fontSize: "14px",
                          fontWeight: "600",
                        }}
                      >
                        📷 Ảnh hiện tại ({blogImages.length}):
                        {(pendingImageUploads.length > 0 ||
                          pendingImageDeletes.length > 0) && (
                          <span
                            style={{
                              fontSize: "12px",
                              color: "#ff7a00",
                              marginLeft: "8px",
                            }}
                          >
                            ({pendingImageUploads.length} mới,{" "}
                            {pendingImageDeletes.length} sẽ xóa)
                          </span>
                        )}
                      </h4>
                      <div
                        style={{
                          display: "flex",
                          flexWrap: "wrap",
                          gap: "12px",
                        }}
                      >
                        {blogImages.map((image) => (
                          <div
                            key={image.imageId}
                            className="image-item"
                            style={{ position: "relative" }}
                          >
                            <Image
                              src={
                                image.isPreview
                                  ? image.imageUrl
                                  : convertGoogleDriveUrl(image.imageUrl)
                              }
                              alt={image.caption || "Blog image"}
                              width={100}
                              height={100}
                              style={{
                                objectFit: "cover",
                                borderRadius: 8,
                                border: image.isPreview
                                  ? "2px dashed #ff7a00"
                                  : "2px solid #e9ecef",
                                opacity: pendingImageDeletes.includes(
                                  image.imageId
                                )
                                  ? 0.5
                                  : 1,
                              }}
                              preview={{
                                mask: (
                                  <div style={{ fontSize: "12px" }}>👁️ Xem</div>
                                ),
                              }}
                            />

                            <Tooltip
                              title={
                                image.isPreview
                                  ? "Hủy thêm ảnh"
                                  : "Đánh dấu xóa"
                              }
                            >
                              <Button
                                danger
                                size="small"
                                icon={<DeleteOutlined />}
                                onClick={() =>
                                  handleImageDelete(
                                    image.imageId,
                                    editingBlog.blogId
                                  )
                                }
                                className="delete-image-btn"
                                style={{
                                  position: "absolute",
                                  top: "-8px",
                                  right: "-8px",
                                  borderRadius: "50%",
                                  width: "24px",
                                  height: "24px",
                                  display: "flex",
                                  alignItems: "center",
                                  justifyContent: "center",
                                  boxShadow: "0 2px 8px rgba(0,0,0,0.15)",
                                  backgroundColor: image.isPreview
                                    ? "#ff7a00"
                                    : undefined,
                                }}
                              />
                            </Tooltip>

                            {/* Status indicator */}
                            <div
                              style={{
                                fontSize: "10px",
                                color: image.isPreview ? "#ff7a00" : "#666",
                                textAlign: "center",
                                marginTop: "4px",
                                fontWeight: "500",
                              }}
                            >
                              {image.isPreview
                                ? "🆕 Mới"
                                : pendingImageDeletes.includes(image.imageId)
                                ? "🗑️ Sẽ xóa"
                                : image.caption && image.caption !== "string"
                                ? image.caption
                                : ""}
                            </div>
                          </div>
                        ))}
                      </div>
                    </div>
                  )}
                </Spin>

                <Upload
                  accept="image/*"
                  beforeUpload={(file) =>
                    handleImageUpload(file, editingBlog.blogId)
                  }
                  showUploadList={false}
                  multiple
                  disabled={blogImageLoading}
                >
                  <Button
                    icon={<PictureOutlined />}
                    style={{
                      borderRadius: "8px",
                      borderStyle: "dashed",
                      height: "40px",
                      width: "100%",
                    }}
                    loading={blogImageLoading}
                  >
                    📸 Thêm ảnh mới
                  </Button>
                </Upload>

                <div
                  style={{
                    fontSize: "12px",
                    color: "#6c757d",
                    marginTop: "8px",
                    textAlign: "center",
                  }}
                >
                  💡 Hỗ trợ JPG, PNG, GIF. Tối đa 10MB mỗi ảnh.
                  {(pendingImageUploads.length > 0 ||
                    pendingImageDeletes.length > 0) && (
                    <div
                      style={{
                        color: "#ff7a00",
                        marginTop: "4px",
                        fontWeight: "500",
                      }}
                    >
                      ⚠️ Thay đổi chỉ được lưu khi bấm "Cập nhật"
                    </div>
                  )}
                </div>
              </div>
            ) : (
              // 🎯 Create new blog - existing code
              <div>
                <Dragger
                  accept="image/*"
                  beforeUpload={(file) => handleImageUpload(file)}
                  showUploadList={false}
                  multiple
                  style={{ marginBottom: "16px" }}
                >
                  <p className="ant-upload-drag-icon">
                    <InboxOutlined />
                  </p>
                  <p className="ant-upload-text">
                    Click hoặc kéo thả ảnh vào đây để upload
                  </p>
                  <p className="ant-upload-hint">
                    Hỗ trợ JPG, PNG, GIF. Kích thước tối đa 10MB.
                  </p>
                </Dragger>

                {uploadingImages.length > 0 && (
                  <div className="uploading-images">
                    <h4>Ảnh đã chọn:</h4>
                    <div className="image-previews">
                      {uploadingImages.map((img) => (
                        <div key={img.uid} className="image-preview">
                          <img
                            src={img.url}
                            alt={img.name}
                            style={{
                              width: 100,
                              height: 100,
                              objectFit: "cover",
                              borderRadius: 8,
                            }}
                          />
                          <Button
                            danger
                            size="small"
                            icon={<DeleteOutlined />}
                            onClick={() => handleRemoveUploadingImage(img.uid)}
                            className="delete-preview-btn"
                          />
                        </div>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            )}
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default Blog;
