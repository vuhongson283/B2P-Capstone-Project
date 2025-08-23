import React, { useState, useEffect } from "react";
import { Modal, Input, Button, Spin, Tag, Dropdown, message } from "antd";
import {
  CloseOutlined,
  CalendarOutlined,
  EditOutlined,
  MessageOutlined,
  ClockCircleOutlined,
  MoreOutlined,
  DeleteOutlined,
  SendOutlined,
} from "@ant-design/icons";
import {
  getAllComments,
  createComment,
  updateComment,
  deleteComment,
  getBlogImages,
} from "../../services/apiService";
import styles from "../Common/CommentModal.module.scss";
import signalRService from "../../services/signalRService";

const { TextArea } = Input;

const CommentModal = ({
  visible,
  blog,
  currentUser,
  userCache,
  blogImagesCache,
  onClose,
  onCommentCountUpdate,
  renderUserAvatar,
  renderBlogImages,
  formatDateTime,
}) => {
  // =============== STATE ===============
  const [comments, setComments] = useState([]);
  const [commentInput, setCommentInput] = useState("");
  const [replyInputs, setReplyInputs] = useState({});
  const [editingComment, setEditingComment] = useState(null);
  const [loadingComments, setLoadingComments] = useState(false);
  const [currentBlogImages, setCurrentBlogImages] = useState([]);
  const [showReplyInputs, setShowReplyInputs] = useState({});
  const [nestedReplyInputs, setNestedReplyInputs] = useState({});
  const [showNestedReplyInputs, setShowNestedReplyInputs] = useState({});

  // =============== EFFECTS ===============
  useEffect(() => {
    if (visible && blog) {
      fetchCommentsForBlog(blog.blogId);
      fetchBlogImagesForModal(blog.blogId);
    }
  }, [visible, blog]);

  useEffect(() => {
    if (visible && blog) {
      window.currentOpenBlogId = blog.blogId;
      window.refreshCommentsCallback = () => fetchCommentsForBlog(blog.blogId);
    }

    return () => {
      window.currentOpenBlogId = null;
      window.refreshCommentsCallback = null;
    };
  }, [visible, blog]);

  // =============== FUNCTIONS ===============
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
      }
    } catch (error) {
      console.error("Error fetching comments:", error);
      message.error("Không thể tải bình luận");
    } finally {
      setLoadingComments(false);
    }
  };

  const fetchBlogImagesForModal = async (blogId) => {
    try {
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
    if (!commentInput.trim() || !blog) return;

    try {
      const commentData = {
        userId: currentUser.userId,
        blogId: blog.blogId,
        content: commentInput.trim(),
        parentCommentId: null,
      };

      const response = await createComment(commentData);
      if (response && response.success) {
        setCommentInput("");
        await fetchCommentsForBlog(blog.blogId);

        const newCount = (blog.totalComments || 0) + 1;
        onCommentCountUpdate(blog.blogId, newCount);

        if (signalRService.connected) {
          const notification = {
            commentId: response.data?.commentId || Date.now(),
            userId: currentUser.userId,
            userName: currentUser.fullName,
            userAvatar: currentUser.avatar,
            blogId: blog.blogId,
            blogTitle: blog.title,
            blogAuthorId: blog.userId,
            content: commentInput.trim(),
            isReply: false,
            timestamp: new Date().toISOString(),
            action: "comment_created",
          };

          try {
            await signalRService.sendCommentNotification(notification);
          } catch (error) {
            console.error("Error sending comment notification:", error);
          }
        }

        message.success("💬 Đã thêm bình luận!");
      }
    } catch (error) {
      console.error("Error creating comment:", error);
      message.error("Có lỗi khi thêm bình luận");
    }
  };

  const handleReplySubmit = async (parentCommentId) => {
    const content = replyInputs[parentCommentId];
    if (!content?.trim() || !blog) return;

    try {
      const replyData = {
        userId: currentUser.userId,
        blogId: blog.blogId,
        content: content.trim(),
        parentCommentId: parentCommentId,
      };

      const response = await createComment(replyData);
      if (response && response.success) {
        setReplyInputs((prev) => ({ ...prev, [parentCommentId]: "" }));
        setShowReplyInputs((prev) => ({ ...prev, [parentCommentId]: false }));

        await fetchCommentsForBlog(blog.blogId);

        const newCount = (blog.totalComments || 0) + 1;
        onCommentCountUpdate(blog.blogId, newCount);

        message.success("↩️ Đã trả lời bình luận!");
      }
    } catch (error) {
      console.error("Error creating reply:", error);
      message.error("Có lỗi khi trả lời bình luận");
    }
  };

  const handleNestedReplySubmit = async (parentReplyId) => {
    const content = nestedReplyInputs[parentReplyId];
    if (!content?.trim() || !blog) return;

    try {
      const replyData = {
        userId: currentUser.userId,
        blogId: blog.blogId,
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

        await fetchCommentsForBlog(blog.blogId);

        const newCount = (blog.totalComments || 0) + 1;
        onCommentCountUpdate(blog.blogId, newCount);

        message.success("↩️ Đã trả lời bình luận!");
      }
    } catch (error) {
      console.error("Error creating nested reply:", error);
      message.error("Có lỗi khi trả lời bình luận");
    }
  };

  const handleCommentEdit = async (commentId, content) => {
    if (!content?.trim() || !blog) return;

    try {
      const commentData = {
        userId: currentUser.userId,
        blogId: blog.blogId,
        content: content.trim(),
        parentCommentId: null,
      };

      const response = await updateComment(commentId, commentData);
      if (response && response.success) {
        setEditingComment(null);
        await fetchCommentsForBlog(blog.blogId);
        message.success("✏️ Cập nhật bình luận thành công!");
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
            await fetchCommentsForBlog(blog.blogId);

            const newCount = Math.max((blog.totalComments || 0) - 1, 0);
            onCommentCountUpdate(blog.blogId, newCount);

            message.success("🗑️ Xóa bình luận thành công!");
          }
        } catch (error) {
          console.error("Error deleting comment:", error);
          message.error("Có lỗi khi xóa bình luận");
        }
      },
    });
  };

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

  // Trong renderNestedReplies function, thay đổi return structure:
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
          {/* Facebook-style reply layout */}
          <div className={styles.facebookReplyLayout}>
            <div className={styles.replyAvatarSection}>
              {renderUserAvatar(reply.userId, "small")}
            </div>

            <div className={styles.replyContentSection}>
              <div className={styles.replyBubbleWrapper}>
                {editingComment === reply.commentId ? (
                  <div className={styles.editComment}>
                    <TextArea
                      defaultValue={reply.content}
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
                          handleCommentEdit(reply.commentId, textarea.value);
                        }}
                      >
                        Lưu
                      </Button>
                    </div>
                  </div>
                ) : (
                  <div className={styles.facebookReplyBubble}>
                    <div className={styles.replyHeader}>
                      <span className={styles.replyAuthor}>
                        {replyUser?.fullName || `User ${reply.userId}`}
                        {reply.userId === currentUser.userId && (
                          <Tag
                            size="small"
                            color="green"
                            style={{ marginLeft: 4 }}
                          >
                            Bạn
                          </Tag>
                        )}
                      </span>
                      {canEditReply && (
                        <Dropdown
                          menu={{
                            items: [
                              {
                                key: "edit",
                                label: "Chỉnh sửa",
                                icon: <EditOutlined />,
                                onClick: () =>
                                  setEditingComment(reply.commentId),
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
                          <button className={styles.replyMenuButton}>
                            <MoreOutlined />
                          </button>
                        </Dropdown>
                      )}
                    </div>
                    <div className={styles.replyText}>{reply.content}</div>
                  </div>
                )}
              </div>

              <div className={styles.facebookReplyMeta}>
                <span className={styles.replyTime}>
                  {formatDateTime(reply.postAt)}
                  {reply.updatedAt && reply.updatedAt !== reply.postAt && (
                    <span className={styles.edited}> · Đã chỉnh sửa</span>
                  )}
                </span>
                <button
                  className={styles.facebookReplyButton}
                  onClick={() => toggleNestedReplyInput(reply.commentId)}
                >
                  {showNestedReply ? "Ẩn" : "Trả lời"}
                </button>
              </div>

              {/* Nested Reply Input */}
              <div
                className={`${styles.facebookNestedReplyInput} ${
                  showNestedReply ? styles.show : ""
                }`}
              >
                <div className={styles.nestedInputWrapper}>
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
                    onPressEnter={() =>
                      handleNestedReplySubmit(reply.commentId)
                    }
                    suffix={
                      <button
                        className={styles.facebookSendButton}
                        onClick={() => handleNestedReplySubmit(reply.commentId)}
                        disabled={!nestedReplyInputs[reply.commentId]?.trim()}
                      >
                        <SendOutlined />
                      </button>
                    }
                  />
                </div>
              </div>

              {/* Further nested replies */}
              <div className={styles.nestedRepliesContainer}>
                {renderNestedReplies(reply.commentId)}
              </div>
            </div>
          </div>
        </div>
      );
    });
  };

  if (!blog) return null;

  const blogUser = userCache[blog.userId];
  const mainComments = comments.filter((comment) => !comment.parentCommentId);

  return (
    <Modal
      title={null}
      open={visible}
      onCancel={onClose}
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
            <h3>Bài viết của {blogUser?.fullName || `User ${blog.userId}`}</h3>
            <div className={styles.subtitle}>
              📅 {new Date().toLocaleDateString("vi-VN")} • 🕐{" "}
              {new Date().toLocaleTimeString("vi-VN")}
            </div>
          </div>
          <button onClick={onClose} className={styles.closeButton}>
            <CloseOutlined />
          </button>
        </div>

        {/* Blog Detail */}
        <div className={styles.blogDetail}>
          <div className={styles.authorInfo}>
            {renderUserAvatar(blog.userId, 50)}
            <div className={styles.authorDetails}>
              <h4>
                {blogUser?.fullName || `User ${blog.userId}`}
                {blog.userId === currentUser.userId && (
                  <Tag size="small" color="green" style={{ marginLeft: 8 }}>
                    🟢 Đang hoạt động
                  </Tag>
                )}
              </h4>
              <div className={styles.postMeta}>
                <span>
                  <CalendarOutlined /> {formatDateTime(blog.postAt)}
                </span>
                {blog.updatedAt && blog.updatedAt !== blog.postAt && (
                  <span className={styles.updated}>
                    <EditOutlined /> Đã chỉnh sửa{" "}
                    {formatDateTime(blog.updatedAt)}
                  </span>
                )}
              </div>
            </div>
          </div>

          <div className={styles.postContent}>
            <h3 className={styles.postTitle}>{blog.title}</h3>
            <div className={styles.postText}>{blog.content}</div>
            {renderBlogImages(currentBlogImages, true)}
          </div>

          <div className={styles.postStats}>
            <Tag icon={<MessageOutlined />}>
              {blog.totalComments || 0} bình luận
            </Tag>
          </div>
        </div>

        {/* Comments Section */}
        <div className={styles.commentsSection}>
          <div className={styles.commentsHeader}>
            <h4>💬 Bình luận ({blog.totalComments || 0})</h4>
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
                    Hãy là người đầu tiên bình luận!{" "}
                    {currentUser?.fullName || "Bạn"} có thể viết ngay ⬇️
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
                    <div key={comment.commentId} className={styles.commentItem}>
                      <div className={styles.commentHeader}>
                        {renderUserAvatar(comment.userId, "small")}
                        <div className={styles.commentInfo}>
                          <div className={styles.commentAuthor}>
                            {commentUser?.fullName || `User ${comment.userId}`}
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

                      <div className={styles.commentActions}>
                        <button
                          className={styles.replyButton}
                          onClick={() => toggleReplyInput(comment.commentId)}
                        >
                          {showReply ? "Ẩn trả lời" : "Trả lời"}
                        </button>
                      </div>

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
                              disabled={!replyInputs[comment.commentId]?.trim()}
                            >
                              <SendOutlined />
                            </button>
                          }
                        />
                      </div>

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
                  placeholder={`Viết bình luận với tư cách ${currentUser.fullName}...`}
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

export default CommentModal;
