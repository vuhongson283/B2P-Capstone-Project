import React, { useState, useEffect, useCallback } from "react";
import ReactDOM from "react-dom";
import "./PaymentManager.scss";
import { useAuth } from "../../contexts/AuthContext";
import {
  getMerchantPaymentsByUserId,
  createMerchantPayment,
  updateMerchantPayment,
} from "../../services/apiService";

// Modal component
const Modal = React.memo(({ children, onClose }) => {
  const handleModalClick = useCallback(
    (e) => {
      if (e.target === e.currentTarget) {
        onClose();
      }
    },
    [onClose]
  );

  useEffect(() => {
    const handleEsc = (e) => {
      if (e.keyCode === 27) onClose();
    };
    document.addEventListener("keydown", handleEsc);
    document.body.style.overflow = "hidden";

    return () => {
      document.removeEventListener("keydown", handleEsc);
      document.body.style.overflow = "unset";
    };
  }, [onClose]);

  const modalRoot = document.getElementById("modal-root");
  if (!modalRoot) return null;

  return ReactDOM.createPortal(
    <div
      style={{
        position: "fixed",
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        backgroundColor: "rgba(0, 0, 0, 0.6)",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        zIndex: 999999,
        padding: "20px",
      }}
      onClick={handleModalClick}
    >
      <div
        style={{
          backgroundColor: "white",
          borderRadius: "12px",
          boxShadow: "0 25px 50px -12px rgba(0, 0, 0, 0.25)",
          width: "100%",
          maxWidth: "500px",
          maxHeight: "90vh",
          overflow: "hidden",
          position: "relative",
        }}
        onClick={(e) => e.stopPropagation()}
      >
        {children}
      </div>
    </div>,
    modalRoot
  );
});

const PaymentManager = () => {
  useEffect(() => {
    document.title = "Quản lý thanh toán - B2P";
  }, []);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [accountType, setAccountType] = useState("");
  const [paymentKey, setPaymentKey] = useState("");
  const [accounts, setAccounts] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [success, setSuccess] = useState(null);

  // User ID - có thể lấy từ context/props hoặc localStorage
  const { userId } = useAuth();

  // Payment method mapping theo API
  const PAYMENT_METHODS = {
    domestic: {
      id: 2,
      name: "ZaloPay",
      displayName: "Quốc nội",
    },
    international: {
      id: 4,
      name: "Stripe",
      displayName: "Quốc tế",
    },
  };

  // Tạo modal element khi component mount
  useEffect(() => {
    const modalRoot = document.getElementById("modal-root");
    if (!modalRoot) {
      const div = document.createElement("div");
      div.id = "modal-root";
      document.body.appendChild(div);
    }
  }, []);

  // Helper function để format date
  const formatDate = (dateString) => {
    if (!dateString) return "";
    try {
      const date = new Date(dateString);
      return date.toLocaleDateString("vi-VN", {
        year: "numeric",
        month: "2-digit",
        day: "2-digit",
      });
    } catch (error) {
      console.error("Error formatting date:", error);
      return "";
    }
  };

  // Helper function để get display name cho account type
  const getAccountTypeDisplay = (paymentMethodName) => {
    if (!paymentMethodName) return "Unknown";

    const lowerMethod = paymentMethodName.toLowerCase();
    if (
      lowerMethod.includes("vnpay") ||
      lowerMethod.includes("momo") ||
      lowerMethod.includes("zalopay")
    ) {
      return "Quốc nội";
    }
    if (lowerMethod.includes("stripe") || lowerMethod.includes("paypal")) {
      return "Quốc tế";
    }
    return paymentMethodName;
  };

  // Fetch payment accounts từ API
  const fetchPaymentAccounts = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      console.log("Fetching payment accounts for user:", userId);
      const response = await getMerchantPaymentsByUserId(userId);

      console.log("Full API Response:", response);
      console.log("Response data:", response?.data);

      // Kiểm tra response structure từ console log
      if (response && response.data && Array.isArray(response.data)) {
        // Transform API data theo structure thực tế
        const transformedAccounts = response.data.map((item) => ({
          id: item.merchantPaymentId,
          type: getAccountTypeDisplay(item.paymentMethodName),
          paymentKey: item.paymentKey || "",
          gateway: item.paymentMethodName || "Unknown",
          createdDate: formatDate(item.createdAt),
          isActive: item.statusId === 1, // statusId 1 = active dựa vào console
          statusId: item.statusId,
          paymentMethodId: item.paymentMethodId,
          userId: item.userId,
        }));

        setAccounts(transformedAccounts);
        console.log("Transformed accounts:", transformedAccounts);
      } else if (
        response?.success &&
        response?.data &&
        Array.isArray(response.data)
      ) {
        // Trường hợp response trực tiếp
        const transformedAccounts = response.data.map((item) => ({
          id: item.merchantPaymentId,
          type: getAccountTypeDisplay(item.paymentMethodName),
          paymentKey: item.paymentKey || "",
          gateway: item.paymentMethodName || "Unknown",
          createdDate: formatDate(item.createdAt),
          isActive: item.statusId === 1,
          statusId: item.statusId,
          paymentMethodId: item.paymentMethodId,
          userId: item.userId,
        }));

        setAccounts(transformedAccounts);
        console.log("Transformed accounts (direct):", transformedAccounts);
      } else {
        console.log(
          "No data received or unexpected response structure:",
          response
        );
        setAccounts([]);
      }
    } catch (err) {
      console.error("Error fetching payment accounts:", err);

      // Xử lý các loại lỗi khác nhau
      let errorMessage =
        "Không thể tải danh sách tài khoản thanh toán. Vui lòng thử lại.";

      if (err.response) {
        // Server responded with error status
        const status = err.response.status;
        const data = err.response.data;

        if (status === 404) {
          errorMessage = "Không tìm thấy dữ liệu tài khoản thanh toán.";
          setAccounts([]); // Set empty array for 404
        } else if (status === 401) {
          errorMessage = "Phiên làm việc đã hết hạn. Vui lòng đăng nhập lại.";
        } else if (status === 500) {
          errorMessage = "Lỗi hệ thống. Vui lòng thử lại sau.";
        } else if (data?.message) {
          errorMessage = data.message;
        }
      } else if (err.request) {
        // Network error
        errorMessage = "Lỗi kết nối mạng. Vui lòng kiểm tra kết nối internet.";
      }

      setError(errorMessage);
      if (err.response?.status !== 404) {
        setAccounts([]);
      }
    } finally {
      setLoading(false);
    }
  }, [userId]);

  // Load data khi component mount
  useEffect(() => {
    fetchPaymentAccounts();
  }, [fetchPaymentAccounts]);

  // Toggle account status (Active/Inactive)
  const toggleAccountStatus = useCallback(
    async (id) => {
      try {
        setLoading(true);
        setError(null);

        const account = accounts.find((acc) => acc.id === id);
        if (!account) {
          setError("Không tìm thấy tài khoản.");
          return;
        }

        // Toggle status: Active (1) <-> Inactive (2)
        const newStatusId = account.isActive ? 2 : 1;

        console.log("Updating account status:", {
          id,
          currentStatus: account.isActive,
          newStatusId,
          paymentKey: account.paymentKey,
        });

        // Gọi API update theo format từ hình
        const updatePayload = {
          paymentKey: account.paymentKey,
          statusId: newStatusId,
        };

        const response = await updateMerchantPayment(id, updatePayload);
        console.log("Update response:", response);

        // Kiểm tra response theo nhiều format khác nhau
        let isSuccess = false;

        if (response?.data?.success) {
          isSuccess = true;
        } else if (response?.success) {
          isSuccess = true;
        } else if (response?.status === 200 || response?.status === 201) {
          isSuccess = true;
        } else if (response?.data?.status === 200) {
          isSuccess = true;
        }

        console.log("Update success check:", isSuccess);

        if (isSuccess) {
          // Update local state
          setAccounts((prevAccounts) =>
            prevAccounts.map((acc) =>
              acc.id === id
                ? { ...acc, isActive: !acc.isActive, statusId: newStatusId }
                : acc
            )
          );

          setSuccess(
            `Trạng thái tài khoản đã được cập nhật thành ${
              newStatusId === 1 ? "Active" : "Inactive"
            }.`
          );

          // Auto clear success message
          setTimeout(() => setSuccess(null), 3000);
        } else {
          // Kiểm tra xem có thực sự update thành công không
          console.warn(
            "Response không có success flag nhưng có thể đã update thành công. Refreshing data..."
          );

          // Update local state vì có thể đã thành công
          setAccounts((prevAccounts) =>
            prevAccounts.map((acc) =>
              acc.id === id
                ? { ...acc, isActive: !acc.isActive, statusId: newStatusId }
                : acc
            )
          );

          setSuccess(
            "Trạng thái tài khoản có thể đã được cập nhật. Đang làm mới dữ liệu..."
          );

          // Refresh để đảm bảo data đúng
          setTimeout(async () => {
            await fetchPaymentAccounts();
            setSuccess(null);
          }, 1000);
        }
      } catch (err) {
        console.error("Error updating account status:", err);

        // Kiểm tra xem có thực sự là lỗi không
        if (err.response?.status === 200 || err.response?.status === 201) {
          console.log("Actually updated successfully despite error");

          const account = accounts.find((acc) => acc.id === id);
          const newStatusId = account.isActive ? 2 : 1;

          setAccounts((prevAccounts) =>
            prevAccounts.map((acc) =>
              acc.id === id
                ? { ...acc, isActive: !acc.isActive, statusId: newStatusId }
                : acc
            )
          );

          setSuccess(
            `Trạng thái tài khoản đã được cập nhật thành ${
              newStatusId === 1 ? "Active" : "Inactive"
            }.`
          );
          setTimeout(() => setSuccess(null), 3000);
          return;
        }

        let errorMessage = "Không thể cập nhật trạng thái tài khoản.";
        if (err.response?.data?.message) {
          errorMessage = err.response.data.message;
        } else if (err.response?.status === 404) {
          errorMessage = "Không tìm thấy tài khoản cần cập nhật.";
        } else if (err.response?.status === 401) {
          errorMessage = "Không có quyền cập nhật tài khoản này.";
        }

        setError(errorMessage);
      } finally {
        setLoading(false);
      }
    },
    [accounts, fetchPaymentAccounts]
  );

  // Tạo tài khoản thanh toán mới
  const handleCreateAccount = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const selectedMethod = PAYMENT_METHODS[accountType];
      if (!selectedMethod) {
        setError("Loại tài khoản không hợp lệ.");
        return;
      }

      // Prepare payload theo format API từ hình
      const createPayload = {
        userId: userId,
        paymentMethodId: selectedMethod.id,
        paymentKey: paymentKey.trim(),
        statusId: 1, // Active by default
      };

      console.log("Creating payment account with payload:", createPayload);

      const response = await createMerchantPayment(createPayload);
      console.log("Create response:", response);

      // Kiểm tra response theo nhiều format khác nhau
      let isSuccess = false;

      if (response?.data?.success) {
        isSuccess = true;
      } else if (response?.success) {
        isSuccess = true;
      } else if (response?.status === 201 || response?.status === 200) {
        isSuccess = true;
      } else if (response?.data?.status === 201) {
        isSuccess = true;
      }

      console.log("Create success check:", isSuccess);

      if (isSuccess) {
        // Reset form
        setShowCreateModal(false);
        setAccountType("");
        setPaymentKey("");

        setSuccess("Tài khoản thanh toán đã được tạo thành công!");

        // Refresh data từ API để lấy dữ liệu mới nhất
        await fetchPaymentAccounts();

        // Auto clear success message
        setTimeout(() => setSuccess(null), 3000);
      } else {
        // Kiểm tra xem có thực sự tạo thành công không bằng cách check console
        console.warn(
          "Response không có success flag nhưng có thể đã tạo thành công. Refreshing data..."
        );

        // Thử refresh data xem có tạo thành công không
        await fetchPaymentAccounts();

        // Reset form vì có thể đã tạo thành công
        setShowCreateModal(false);
        setAccountType("");
        setPaymentKey("");

        setSuccess(
          "Tài khoản thanh toán có thể đã được tạo. Vui lòng kiểm tra danh sách."
        );
        setTimeout(() => setSuccess(null), 5000);
      }
    } catch (err) {
      console.error("Error creating payment account:", err);

      // Kiểm tra xem có thực sự là lỗi không
      if (err.response?.status === 201 || err.response?.status === 200) {
        console.log("Actually created successfully despite error");

        setShowCreateModal(false);
        setAccountType("");
        setPaymentKey("");

        setSuccess("Tài khoản thanh toán đã được tạo thành công!");
        await fetchPaymentAccounts();
        setTimeout(() => setSuccess(null), 3000);
        return;
      }

      let errorMessage =
        "Không thể tạo tài khoản thanh toán. Vui lòng thử lại.";

      if (err.response?.data?.message) {
        errorMessage = err.response.data.message;
      } else if (err.response?.status === 400) {
        errorMessage = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại thông tin.";
      } else if (err.response?.status === 409) {
        errorMessage =
          "Payment key đã tồn tại. Vui lòng sử dụng payment key khác.";
      } else if (err.response?.status === 401) {
        errorMessage = "Không có quyền tạo tài khoản thanh toán.";
      }

      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  }, [accountType, paymentKey, userId, fetchPaymentAccounts]);

  // Stable handlers
  const handleCloseModal = useCallback(() => {
    setShowCreateModal(false);
    setAccountType("");
    setPaymentKey("");
    setError(null); // Clear error when closing modal
  }, []);

  const handlePaymentKeyChange = useCallback((e) => {
    setPaymentKey(e.target.value);
  }, []);

  const handleAccountTypeChange = useCallback((e) => {
    setAccountType(e.target.value);
  }, []);

  const handlePaste = useCallback(async () => {
    try {
      const text = await navigator.clipboard.readText();
      setPaymentKey(text.trim());
    } catch (err) {
      console.error("Failed to read clipboard contents: ", err);
      alert("Không thể truy cập clipboard. Vui lòng paste bằng Ctrl+V");
    }
  }, []);

  const openModal = useCallback(() => {
    setShowCreateModal(true);
    setError(null); // Clear error when opening modal
  }, []);

  const handleRefresh = useCallback(() => {
    setError(null);
    setSuccess(null);
    fetchPaymentAccounts();
  }, [fetchPaymentAccounts]);

  const dismissError = useCallback(() => {
    setError(null);
  }, []);

  const dismissSuccess = useCallback(() => {
    setSuccess(null);
  }, []);

  return (
    <div className="payment-manager">
      <div className="container">
        <div className="card">
          {/* Header */}
          <div className="card-header">
            <h1 className="title">Quản lý tài khoản thanh toán</h1>
            <button
              onClick={handleRefresh}
              disabled={loading}
              style={{
                padding: "8px 12px",
                border: "1px solid #d1d5db",
                borderRadius: "6px",
                backgroundColor: loading ? "#f3f4f6" : "white",
                color: "#374151",
                cursor: loading ? "not-allowed" : "pointer",
                fontSize: "14px",
                marginLeft: "12px",
              }}
            >
              {loading ? "🔄 Đang tải..." : "🔄 Làm mới"}
            </button>
          </div>

          {/* Success Message */}
          {success && (
            <div
              style={{
                backgroundColor: "#d1fae5",
                border: "1px solid #a7f3d0",
                color: "#065f46",
                padding: "12px",
                borderRadius: "6px",
                margin: "16px 0",
                fontSize: "14px",
              }}
            >
              {success}
              <button
                onClick={dismissSuccess}
                style={{
                  float: "right",
                  background: "none",
                  border: "none",
                  color: "#065f46",
                  cursor: "pointer",
                  fontSize: "16px",
                  padding: "0 4px",
                }}
              >
                ×
              </button>
            </div>
          )}

          {/* Error Message */}
          {error && (
            <div
              style={{
                backgroundColor: "#fee2e2",
                border: "1px solid #fecaca",
                color: "#dc2626",
                padding: "12px",
                borderRadius: "6px",
                margin: "16px 0",
                fontSize: "14px",
              }}
            >
              {error}
              <button
                onClick={dismissError}
                style={{
                  float: "right",
                  background: "none",
                  border: "none",
                  color: "#dc2626",
                  cursor: "pointer",
                  fontSize: "16px",
                  padding: "0 4px",
                }}
              >
                ×
              </button>
            </div>
          )}

          {/* Create Button */}
          <div className="create-button-section">
            <button
              onClick={openModal}
              disabled={loading}
              className="btn btn-primary"
              style={{
                opacity: loading ? 0.6 : 1,
                cursor: loading ? "not-allowed" : "pointer",
              }}
            >
              Tạo tài khoản thanh toán mới
            </button>
          </div>

          {/* Table */}
          <div className="table-container">
            {/* Table Header */}
            <div className="table-header">
              <div className="table-row">
                <div className="table-cell text-center">STT</div>
                <div className="table-cell">Loại tài khoản</div>
                <div className="table-cell">PaymentKey</div>
                <div className="table-cell">Cổng thanh toán</div>
                <div className="table-cell">Ngày tạo</div>
                <div className="table-cell text-center">Trạng thái</div>
              </div>
            </div>

            {/* Table Content */}
            <div className="table-content">
              {loading ? (
                <div className="empty-state">
                  <div className="empty-message">🔄 Đang tải dữ liệu...</div>
                </div>
              ) : accounts.length > 0 ? (
                <div className="table-body">
                  {accounts.map((account, index) => (
                    <div key={account.id} className="table-data-row">
                      <div className="table-cell text-center">{index + 1}</div>
                      <div className="table-cell">{account.type}</div>
                      <div
                        className="table-cell payment-key"
                        title={account.paymentKey}
                      >
                        {account.paymentKey}
                      </div>
                      <div className="table-cell">{account.gateway}</div>
                      <div className="table-cell">{account.createdDate}</div>
                      <div className="table-cell text-center">
                        <button
                          onClick={() => toggleAccountStatus(account.id)}
                          disabled={loading}
                          className={`toggle-switch ${
                            account.isActive ? "active" : ""
                          }`}
                          style={{
                            opacity: loading ? 0.6 : 1,
                            cursor: loading ? "not-allowed" : "pointer",
                          }}
                        >
                          <span className="toggle-slider" />
                        </button>
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <div className="empty-state">
                  <div className="empty-message">
                    {error
                      ? "Có lỗi xảy ra khi tải dữ liệu"
                      : 'Bạn chưa có tài khoản thanh toán. Click vào nút "Tạo tài khoản thanh toán mới" để bắt đầu.'}
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>

        {/* Modal */}
        {showCreateModal && (
          <Modal onClose={handleCloseModal}>
            {/* Modal Header */}
            <div
              style={{
                padding: "24px 24px 0",
                borderBottom: "1px solid #e5e7eb",
                display: "flex",
                alignItems: "center",
                justifyContent: "space-between",
              }}
            >
              <h2
                style={{
                  fontSize: "18px",
                  fontWeight: "600",
                  color: "#1f2937",
                  margin: 0,
                }}
              >
                Tạo tài khoản thanh toán mới
              </h2>
              <button
                onClick={handleCloseModal}
                disabled={loading}
                style={{
                  background: "none",
                  border: "none",
                  fontSize: "24px",
                  color: "#6b7280",
                  cursor: loading ? "not-allowed" : "pointer",
                  padding: "4px",
                  lineHeight: 1,
                  opacity: loading ? 0.6 : 1,
                }}
              >
                ×
              </button>
            </div>

            {/* Modal Body */}
            <div style={{ padding: "24px" }}>
              {/* Account Type Select */}
              <div style={{ marginBottom: "16px" }}>
                <label
                  style={{
                    display: "block",
                    fontSize: "14px",
                    fontWeight: "500",
                    color: "#374151",
                    marginBottom: "6px",
                  }}
                >
                  Loại tài khoản <span style={{ color: "#dc2626" }}>*</span>
                </label>
                <select
                  value={accountType}
                  onChange={handleAccountTypeChange}
                  disabled={loading}
                  style={{
                    width: "100%",
                    padding: "8px 12px",
                    border: "1px solid #d1d5db",
                    borderRadius: "6px",
                    fontSize: "14px",
                    backgroundColor: loading ? "#f3f4f6" : "white",
                    cursor: loading ? "not-allowed" : "pointer",
                  }}
                >
                  <option value="">Chọn loại tài khoản</option>
                  <option value="domestic">Quốc nội (ZaloPay)</option>
                  <option value="international">Quốc tế (Stripe)</option>
                </select>
              </div>

              {/* Payment Key Input với Paste Button */}
              <div style={{ marginBottom: "16px" }}>
                <label
                  style={{
                    display: "block",
                    fontSize: "14px",
                    fontWeight: "500",
                    color: "#374151",
                    marginBottom: "6px",
                  }}
                >
                  Payment Key <span style={{ color: "#dc2626" }}>*</span>
                </label>
                <div
                  style={{
                    display: "flex",
                    gap: "8px",
                    alignItems: "stretch",
                  }}
                >
                  <input
                    type="text"
                    value={paymentKey}
                    onChange={handlePaymentKeyChange}
                    disabled={loading}
                    placeholder="Nhập payment key"
                    style={{
                      flex: 1,
                      padding: "8px 12px",
                      border: "1px solid #d1d5db",
                      borderRadius: "6px",
                      fontSize: "14px",
                      backgroundColor: loading ? "#f3f4f6" : "white",
                      cursor: loading ? "not-allowed" : "text",
                    }}
                  />
                  <button
                    type="button"
                    onClick={handlePaste}
                    disabled={loading}
                    style={{
                      padding: "8px 12px",
                      border: "1px solid #d1d5db",
                      borderRadius: "6px",
                      backgroundColor: loading ? "#f3f4f6" : "#f9fafb",
                      color: "#374151",
                      cursor: loading ? "not-allowed" : "pointer",
                      fontSize: "12px",
                      whiteSpace: "nowrap",
                      transition: "background-color 0.2s",
                    }}
                    onMouseEnter={(e) => {
                      if (!loading) e.target.style.backgroundColor = "#f3f4f6";
                    }}
                    onMouseLeave={(e) => {
                      if (!loading) e.target.style.backgroundColor = "#f9fafb";
                    }}
                  >
                    📋 Paste
                  </button>
                </div>
              </div>

              {/* Merchant Guide */}
              <div
                style={{
                  backgroundColor: "#eff6ff",
                  padding: "12px",
                  borderRadius: "6px",
                  marginBottom: "16px",
                }}
              >
                <div
                  style={{
                    fontSize: "14px",
                    color: "#4b5563",
                    marginBottom: "8px",
                  }}
                >
                  Chưa có tài khoản merchant?
                </div>
                <button
                  onClick={() => window.open("#", "_blank")}
                  style={{
                    background: "none",
                    border: "none",
                    color: "#3b82f6",
                    fontSize: "14px",
                    cursor: "pointer",
                    textDecoration: "underline",
                    padding: 0,
                  }}
                >
                  Xem hướng dẫn đăng ký tại đây →
                </button>
              </div>
            </div>

            {/* Modal Footer */}
            <div
              style={{
                padding: "16px 24px",
                borderTop: "1px solid #e5e7eb",
                display: "flex",
                justifyContent: "flex-end",
                gap: "12px",
              }}
            >
              <button
                onClick={handleCloseModal}
                disabled={loading}
                style={{
                  padding: "8px 16px",
                  border: "1px solid #d1d5db",
                  borderRadius: "6px",
                  backgroundColor: "white",
                  color: "#374151",
                  cursor: loading ? "not-allowed" : "pointer",
                  fontSize: "14px",
                  opacity: loading ? 0.6 : 1,
                }}
              >
                Hủy
              </button>
              <button
                onClick={handleCreateAccount}
                disabled={!accountType || !paymentKey.trim() || loading}
                style={{
                  padding: "8px 16px",
                  border: "none",
                  borderRadius: "6px",
                  backgroundColor:
                    !accountType || !paymentKey.trim() || loading
                      ? "#9ca3af"
                      : "#3b82f6",
                  color: "white",
                  cursor:
                    !accountType || !paymentKey.trim() || loading
                      ? "not-allowed"
                      : "pointer",
                  fontSize: "14px",
                }}
              >
                {loading ? "Đang tạo..." : "Tạo tài khoản"}
              </button>
            </div>
          </Modal>
        )}
      </div>
    </div>
  );
};

export default PaymentManager;
