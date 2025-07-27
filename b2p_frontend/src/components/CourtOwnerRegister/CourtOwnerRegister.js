import React, { useState, useEffect } from 'react';
import { User, Phone, Mail, MapPin, Lock, Eye, EyeOff, Building, CheckCircle, X } from 'lucide-react';
import './CourtOwnerRegister.scss';
import { registerCourtOwner } from "../../services/apiService";

const PartnerRegistration = () => {
    const [formData, setFormData] = useState({
        email: '',
        password: '',
        confirmPassword: '',
        fullName: '',
        phoneNumber: '',
        isMale: true,
        province: '',
        district: '',
        detailAddress: ''
    });

    const [showPassword, setShowPassword] = useState(false);
    const [showConfirmPassword, setShowConfirmPassword] = useState(false);
    const [errors, setErrors] = useState({});
    const [showSuccessModal, setShowSuccessModal] = useState(false);
    const [isSubmitting, setIsSubmitting] = useState(false);

    // State cho tỉnh/thành phố và quận/huyện
    const [provinces, setProvinces] = useState([]);
    const [districts, setDistricts] = useState([]);

    // Lấy danh sách tỉnh/thành phố
    const fetchProvinces = async () => {
        try {
            const response = await fetch("https://provinces.open-api.vn/api/p/");
            const data = await response.json();
            setProvinces(data);
        } catch (error) {
            console.error("Error fetching provinces:", error);
        }
    };

    // Lấy danh sách quận/huyện theo tỉnh được chọn
    const fetchDistricts = async (provinceName) => {
        if (!provinceName) return;

        try {
            // Tìm province code từ name
            const selectedProvinceObj = provinces.find(
                (p) => p.name === provinceName
            );
            if (!selectedProvinceObj) return;

            const response = await fetch(
                `https://provinces.open-api.vn/api/p/${selectedProvinceObj.code}?depth=2`
            );
            const data = await response.json();
            const districtList = data.districts || [];
            setDistricts(districtList);

            // Reset district khi thay đổi tỉnh
            setFormData(prev => ({ ...prev, district: '' }));
        } catch (error) {
            console.error("Error fetching districts:", error);
        }
    };

    useEffect(() => {
        fetchProvinces();
    }, []);

    // Fetch districts khi selectedProvince thay đổi
    useEffect(() => {
        if (formData.province && provinces.length > 0) {
            fetchDistricts(formData.province);
        } else {
            setDistricts([]);
            setFormData(prev => ({ ...prev, district: '' }));
        }
    }, [formData.province, provinces]);

    const handleInputChange = (e) => {
        const { name, value, type, checked } = e.target;
        setFormData(prev => ({
            ...prev,
            [name]: type === 'checkbox' ? checked : value
        }));

        // Clear error when user starts typing
        if (errors[name]) {
            setErrors(prev => ({
                ...prev,
                [name]: ''
            }));
        }
    };

    const validateForm = () => {
        const newErrors = {};

        if (!formData.email) {
            newErrors.email = 'Email là bắt buộc';
        } else if (!/\S+@\S+\.\S+/.test(formData.email)) {
            newErrors.email = 'Email không hợp lệ';
        }

        if (!formData.password) {
            newErrors.password = 'Mật khẩu là bắt buộc';
        } else if (formData.password.length < 6) {
            newErrors.password = 'Mật khẩu phải có ít nhất 6 ký tự';
        }

        if (!formData.confirmPassword) {
            newErrors.confirmPassword = 'Xác nhận mật khẩu là bắt buộc';
        } else if (formData.password !== formData.confirmPassword) {
            newErrors.confirmPassword = 'Mật khẩu xác nhận không khớp';
        }

        if (!formData.fullName) {
            newErrors.fullName = 'Họ và tên là bắt buộc';
        }

        if (!formData.phoneNumber) {
            newErrors.phoneNumber = 'Số điện thoại là bắt buộc';
        } else if (!/^[0-9]{10,11}$/.test(formData.phoneNumber)) {
            newErrors.phoneNumber = 'Số điện thoại không hợp lệ';
        }

        if (!formData.province) {
            newErrors.province = 'Tỉnh/Thành phố là bắt buộc';
        }

        if (!formData.district) {
            newErrors.district = 'Quận/Huyện là bắt buộc';
        }

        if (!formData.detailAddress) {
            newErrors.detailAddress = 'Địa chỉ chi tiết là bắt buộc';
        }

        setErrors(newErrors);
        return Object.keys(newErrors).length === 0;
    };

    // Thêm vào phần khai báo state (sau dòng const [isSubmitting, setIsSubmitting] = useState(false);)
    const [apiError, setApiError] = useState('');

    // Sửa lại handleSubmit đơn giản hơn
    const handleSubmit = async (e) => {
        e.preventDefault();

        if (!validateForm()) return;

        setIsSubmitting(true);
        setApiError(''); // Clear previous API errors

        // Clear existing field errors
        setErrors(prev => ({
            ...prev,
            email: '',
            phoneNumber: ''
        }));

        // Gộp địa chỉ đầy đủ
        const fullAddress = `${formData.detailAddress}, ${formData.district}, ${formData.province}`;

        // Tạo payload đúng schema của API
        const payload = {
            email: formData.email,
            password: formData.password,
            confirmPassword: formData.confirmPassword,
            fullName: formData.fullName,
            phoneNumber: formData.phoneNumber,
            isMale: formData.isMale,
            address: fullAddress
        };

        try {
            const res = await registerCourtOwner(payload);

            console.log("API Response:", res);

            // Kiểm tra response structure dựa trên log
            if (res.success === false && res.status === 400) {
                // Xử lý lỗi dựa trên message
                const errorMessage = res.message;

                if (errorMessage.toLowerCase().includes('email')) {
                    // Lỗi liên quan đến email
                    setErrors(prev => ({
                        ...prev,
                        email: errorMessage
                    }));
                } else if (errorMessage.toLowerCase().includes('số điện thoại') || errorMessage.toLowerCase().includes('phone')) {
                    // Lỗi liên quan đến số điện thoại
                    setErrors(prev => ({
                        ...prev,
                        phoneNumber: errorMessage
                    }));
                } else {
                    // Lỗi chung khác
                    setApiError(errorMessage);
                }
                return;
            }

            // Nếu thành công
            if (res.success === true && (res.status === 200 || res.status === 201)) {
                console.log("Registration successful!");
                setShowSuccessModal(true);
            } else {
                console.log("Unexpected response:", res);
                setApiError("Phản hồi không mong đợi từ server.");
            }

        } catch (err) {
            console.error("Registration error:", err);

            // Fallback error handling
            if (err.response) {
                const errorData = err.response.data;
                const errorMessage = errorData?.message || "Đăng ký thất bại. Vui lòng thử lại.";
                setApiError(errorMessage);
            } else if (err.request) {
                setApiError("Không thể kết nối đến server. Vui lòng kiểm tra kết nối mạng.");
            } else {
                setApiError("Có lỗi xảy ra. Vui lòng thử lại.");
            }
        } finally {
            setIsSubmitting(false);
        }
    };

    // Thêm vào JSX trước Submit Button (trong form):
    {
        apiError && (
            <div className="form-group">
                <div style={{
                    padding: '12px',
                    backgroundColor: '#fee',
                    border: '1px solid #fcc',
                    borderRadius: '4px',
                    marginBottom: '16px'
                }}>
                    <p style={{
                        color: '#c33',
                        margin: '0',
                        fontSize: '14px'
                    }}>
                        {apiError}
                    </p>
                </div>
            </div>
        )
    }

    const handleModalClose = () => {
        setShowSuccessModal(false);
        // Reset form
        setFormData({
            email: '',
            password: '',
            confirmPassword: '',
            fullName: '',
            phoneNumber: '',
            isMale: true,
            province: '',
            district: '',
            detailAddress: ''
        });
        setErrors({});
    };

    const handleGoHome = () => {
        setShowSuccessModal(false);
        // Add your navigation logic here
        // window.location.href = '/';
        // or using React Router: navigate('/');
    };

    return (
        <div className="registration-container">
            <div className="registration-wrapper">
                {/* Header */}
                <div className="header">
                    <div className="logo-container">
                        <div className="logo-icon">
                            <Building />
                        </div>
                        <span className="logo-text">Book2Play</span>
                    </div>
                    <h1 className="title">Đăng Ký Đối Tác</h1>
                    <p className="subtitle">Tham gia cùng chúng tôi để phát triển cộng đồng thể thao</p>
                </div>

                {/* Registration Form */}
                <div className="form-container">
                    <div className="form-header">
                        <h2 className="form-title">
                            <User />
                            Thông Tin Đăng Ký
                        </h2>
                    </div>

                    <div className="form-content">
                        {/* Email */}
                        <div className="form-group">
                            <label className="label">
                                Email <span className="required">*</span>
                            </label>
                            <div className="input-wrapper">
                                <Mail className="input-icon" />
                                <input
                                    type="email"
                                    name="email"
                                    value={formData.email}
                                    onChange={handleInputChange}
                                    className={`input-field ${errors.email ? 'error' : ''}`}
                                    placeholder="example@email.com"
                                />
                            </div>
                            {errors.email && <p className="error-message">{errors.email}</p>}
                        </div>

                        {/* Password */}
                        <div className="grid-2">
                            <div className="form-group">
                                <label className="label">
                                    Mật khẩu <span className="required">*</span>
                                </label>
                                <div className="input-wrapper">
                                    <Lock className="input-icon" />
                                    <input
                                        type={showPassword ? 'text' : 'password'}
                                        name="password"
                                        value={formData.password}
                                        onChange={handleInputChange}
                                        className={`input-field ${errors.password ? 'error' : ''}`}
                                        placeholder="••••••••"
                                    />
                                    <button
                                        type="button"
                                        onClick={() => setShowPassword(!showPassword)}
                                        className="password-toggle"
                                    >
                                        {showPassword ? <EyeOff /> : <Eye />}
                                    </button>
                                </div>
                                {errors.password && <p className="error-message">{errors.password}</p>}
                            </div>

                            <div className="form-group">
                                <label className="label">
                                    Xác nhận mật khẩu <span className="required">*</span>
                                </label>
                                <div className="input-wrapper">
                                    <Lock className="input-icon" />
                                    <input
                                        type={showConfirmPassword ? 'text' : 'password'}
                                        name="confirmPassword"
                                        value={formData.confirmPassword}
                                        onChange={handleInputChange}
                                        className={`input-field ${errors.confirmPassword ? 'error' : ''}`}
                                        placeholder="••••••••"
                                    />
                                    <button
                                        type="button"
                                        onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                                        className="password-toggle"
                                    >
                                        {showConfirmPassword ? <EyeOff /> : <Eye />}
                                    </button>
                                </div>
                                {errors.confirmPassword && <p className="error-message">{errors.confirmPassword}</p>}
                            </div>
                        </div>

                        {/* Full Name */}
                        <div className="form-group">
                            <label className="label">
                                Họ và tên <span className="required">*</span>
                            </label>
                            <div className="input-wrapper">
                                <User className="input-icon" />
                                <input
                                    type="text"
                                    name="fullName"
                                    value={formData.fullName}
                                    onChange={handleInputChange}
                                    className={`input-field ${errors.fullName ? 'error' : ''}`}
                                    placeholder="Nguyễn Văn A"
                                />
                            </div>
                            {errors.fullName && <p className="error-message">{errors.fullName}</p>}
                        </div>

                        {/* Phone Number and Gender */}
                        <div className="grid-3">
                            <div className="form-group">
                                <label className="label">
                                    Số điện thoại <span className="required">*</span>
                                </label>
                                <div className="input-wrapper">
                                    <Phone className="input-icon" />
                                    <input
                                        type="tel"
                                        name="phoneNumber"
                                        value={formData.phoneNumber}
                                        onChange={handleInputChange}
                                        className={`input-field ${errors.phoneNumber ? 'error' : ''}`}
                                        placeholder="0123456789"
                                    />
                                </div>
                                {errors.phoneNumber && <p className="error-message">{errors.phoneNumber}</p>}
                            </div>

                            <div className="form-group">
                                <label className="label">Giới tính</label>
                                <div className="gender-group">
                                    <label className="radio-item">
                                        <input
                                            type="radio"
                                            name="isMale"
                                            value={true}
                                            checked={formData.isMale === true}
                                            onChange={() => setFormData(prev => ({ ...prev, isMale: true }))}
                                        />
                                        <span>Nam</span>
                                    </label>
                                    <label className="radio-item">
                                        <input
                                            type="radio"
                                            name="isMale"
                                            value={false}
                                            checked={formData.isMale === false}
                                            onChange={() => setFormData(prev => ({ ...prev, isMale: false }))}
                                        />
                                        <span>Nữ</span>
                                    </label>
                                </div>
                            </div>
                        </div>

                        {/* Province and District */}
                        <div className="grid-2">
                            <div className="form-group">
                                <label className="label">
                                    Tỉnh/Thành phố <span className="required">*</span>
                                </label>
                                <div className="input-wrapper">
                                    <MapPin className="input-icon" />
                                    <select
                                        name="province"
                                        value={formData.province}
                                        onChange={handleInputChange}
                                        className={`input-field select ${errors.province ? 'error' : ''}`}
                                    >
                                        <option value="">Chọn tỉnh/thành phố</option>
                                        {provinces.map((province) => (
                                            <option key={province.code} value={province.name}>
                                                {province.name}
                                            </option>
                                        ))}
                                    </select>
                                </div>
                                {errors.province && <p className="error-message">{errors.province}</p>}
                            </div>

                            <div className="form-group">
                                <label className="label">
                                    Quận/Huyện <span className="required">*</span>
                                </label>
                                <div className="input-wrapper">
                                    <MapPin className="input-icon" />
                                    <select
                                        name="district"
                                        value={formData.district}
                                        onChange={handleInputChange}
                                        disabled={!formData.province || districts.length === 0}
                                        className={`input-field select ${errors.district ? 'error' : ''} ${(!formData.province || districts.length === 0) ? 'disabled' : ''}`}
                                    >
                                        <option value="">Chọn quận/huyện</option>
                                        {districts.map((district) => (
                                            <option key={district.code} value={district.name}>
                                                {district.name}
                                            </option>
                                        ))}
                                    </select>
                                </div>
                                {errors.district && <p className="error-message">{errors.district}</p>}
                            </div>
                        </div>

                        {/* Detail Address */}
                        <div className="form-group">
                            <label className="label">
                                Địa chỉ chi tiết <span className="required">*</span>
                            </label>
                            <div className="input-wrapper">
                                <MapPin className="input-icon" />
                                <textarea
                                    name="detailAddress"
                                    value={formData.detailAddress}
                                    onChange={handleInputChange}
                                    className={`input-field textarea ${errors.detailAddress ? 'error' : ''}`}
                                    placeholder="Số nhà, tên đường..."
                                    rows={3}
                                />
                            </div>
                            {errors.detailAddress && <p className="error-message">{errors.detailAddress}</p>}
                        </div>

                        {/* Submit Button */}
                        <div className="form-group">
                            <button
                                onClick={handleSubmit}
                                disabled={isSubmitting}
                                className={`submit-button ${isSubmitting ? 'loading' : ''}`}
                            >
                                {isSubmitting ? 'Đang xử lý...' : 'Đăng Ký Đối Tác'}
                            </button>
                        </div>

                        {/* Terms */}
                        <div className="terms">
                            <p>
                                Bằng việc đăng ký, bạn đồng ý với{' '}
                                <a href="#" className="link">
                                    Điều khoản sử dụng
                                </a>{' '}
                                và{' '}
                                <a href="#" className="link">
                                    Chính sách bảo mật
                                </a>{' '}
                                của Book2Play
                            </p>
                        </div>
                    </div>
                </div>

                {/* Back to Home */}
                <div className="back-link">
                    <a href="#" className="link">
                        ← Quay về trang chủ
                    </a>
                </div>
            </div>

            {/* Success Modal */}
            {showSuccessModal && (
                <div className="modal-overlay">
                    <div className="success-modal">
                        <button
                            onClick={handleModalClose}
                            className="modal-close-btn"
                        >
                            <X />
                        </button>

                        <div className="modal-content">
                            {/* Success Icon */}
                            <div className="success-icon">
                                <CheckCircle />
                            </div>

                            {/* Success Message */}
                            <h3 className="success-title">
                                Đăng ký thành công
                            </h3>
                            <p className="success-message">
                                Cảm ơn bạn đã đăng ký làm đối tác với Book2Play.
                                Vui lòng hoàn thành đủ thông tin trong quản lý tài khoản để hoàn tất quá trình trở thành Chủ Sân
                            </p>

                            {/* Action Buttons */}
                            <div className="modal-actions">
                                <button
                                    onClick={handleModalClose}
                                    className="btn-primary"
                                >
                                    Đăng ký thêm đối tác khác
                                </button>
                                <button
                                    onClick={handleGoHome}
                                    className="btn-secondary"
                                >
                                    Về trang chủ
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};

export default PartnerRegistration;