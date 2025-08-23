import React, { useState, useEffect } from "react";
import {
  Modal,
  Form,
  Input,
  Upload,
  Button,
  Image,
  Tooltip,
  Spin,
  message,
} from "antd";
import {
  PictureOutlined,
  DeleteOutlined,
  InboxOutlined,
} from "@ant-design/icons";
import {
  createBlog,
  updateBlog,
  uploadBlogImage,
  getBlogImages,
  deleteImage,
} from "../../services/apiService";

const { TextArea } = Input;
const { Dragger } = Upload;

const convertGoogleDriveUrl = (url) => {
  if (!url) return "";
  const fileIdMatch = url.match(/[?&]id=([a-zA-Z0-9-_]+)/);
  if (fileIdMatch) {
    const fileId = fileIdMatch[1];
    return `https://drive.google.com/thumbnail?id=${fileId}&sz=w800`;
  }
  return url;
};

const BlogModal = ({
  visible,
  editingBlog,
  currentUser,
  onClose,
  onSuccess,
  setBlogImagesCache,
}) => {
  // =============== STATE ===============
  const [form] = Form.useForm();
  const [loading, setLoading] = useState(false);
  const [blogImages, setBlogImages] = useState([]);
  const [blogImageLoading, setBlogImageLoading] = useState(false);
  const [uploadingImages, setUploadingImages] = useState([]);
  const [pendingImageUploads, setPendingImageUploads] = useState([]);
  const [pendingImageDeletes, setPendingImageDeletes] = useState([]);

  // =============== EFFECTS ===============
  useEffect(() => {
    if (visible) {
      setBlogImages([]);
      setUploadingImages([]);
      setPendingImageUploads([]);
      setPendingImageDeletes([]);

      if (editingBlog) {
        form.setFieldsValue({
          title: editingBlog.title,
          content: editingBlog.content,
        });
        fetchBlogImages(editingBlog.blogId);
      } else {
        form.resetFields();
      }
    }
  }, [visible, editingBlog, form]);

  // =============== FUNCTIONS ===============
  const fetchBlogImages = async (blogId) => {
    try {
      setBlogImageLoading(true);
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

  const handleImageDelete = async (imageId, blogId) => {
    if (typeof imageId === "string" && imageId.startsWith("temp_")) {
      setPendingImageUploads((prev) =>
        prev.filter((img) => img.tempId !== imageId)
      );

      setBlogImages((prevImages) =>
        prevImages.filter((img) => img.imageId !== imageId)
      );
      return;
    }

    if (!pendingImageDeletes.includes(imageId)) {
      setPendingImageDeletes((prev) => [...prev, imageId]);
    }

    setBlogImages((prevImages) =>
      prevImages.filter((img) => img.imageId !== imageId)
    );
  };

  const handleRemoveUploadingImage = (uid) => {
    setUploadingImages((prev) => prev.filter((img) => img.uid !== uid));
  };

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
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

          // 1. Delete marked images
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

          // 2. Upload new images
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

          // Show final result message
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

          // Upload images for new blog
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

      // Reset states and close modal
      setPendingImageUploads([]);
      setPendingImageDeletes([]);
      onSuccess();
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

  const renderUserAvatar = () => {
    return (
      <div className="user-avatar-wrapper">
        <img
          src={currentUser.avatar}
          alt={currentUser.fullName}
          style={{
            width: 40,
            height: 40,
            borderRadius: "50%",
            border: "1px solid #e9ecef",
          }}
        />
      </div>
    );
  };

  // =============== RENDER ===============
  return (
    <Modal
      title={
        <div style={{ display: "flex", alignItems: "center", gap: "12px" }}>
          {renderUserAvatar()}
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
      open={visible}
      onOk={handleSubmit}
      onCancel={onClose}
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
      <Form form={form} layout="vertical">
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
                  <div className="blog-images" style={{ marginBottom: "16px" }}>
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
                              image.isPreview ? "Hủy thêm ảnh" : "Đánh dấu xóa"
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
            // Create new blog - existing code
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
  );
};

export default BlogModal;
