import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom'; // Thêm useNavigate
import {
    CheckCircle, Users, Shield, DollarSign, Clock, Award, ArrowRight,
    Star, TrendingUp, Phone, Mail, MapPin, Play, ChevronDown,
    Zap, Heart, Target, Gift
} from 'lucide-react';
import '../Common/CourtOwnerPolicy.scss';

const CourtOwnerPolicy = () => {
    console.log('🚀 CourtOwnerPolicy component rendered!');

    const navigate = useNavigate(); // Khởi tạo navigate

    const [activeSection, setActiveSection] = useState(null);
    const [isVisible, setIsVisible] = useState({
        hero: true,
        stats: true,
        'benefits-title': true,
        'benefits-grid': true,
        'policy-title': true,
        'policy-items': true,
        cta: true
    });

    useEffect(() => {
        console.log('🔧 useEffect running - Setting up intersection observer');

        const observer = new IntersectionObserver(
            (entries) => {
                entries.forEach((entry) => {
                    if (entry.isIntersecting) {
                        console.log(`✅ Element visible: ${entry.target.id}`);
                        setIsVisible(prev => ({
                            ...prev,
                            [entry.target.id]: true
                        }));
                    }
                });
            },
            { threshold: 0.1 }
        );

        // Force all sections to be visible immediately
        setTimeout(() => {
            setIsVisible({
                hero: true,
                stats: true,
                'benefits-title': true,
                'benefits-grid': true,
                'policy-title': true,
                'policy-items': true,
                cta: true
            });
        }, 100);

        const elements = document.querySelectorAll('[id]');
        console.log(`📝 Found ${elements.length} elements with IDs`);

        elements.forEach((el) => {
            observer.observe(el);
        });

        return () => observer.disconnect();
    }, []);

    const toggleSection = (section) => {
        console.log(`🔄 Toggling section: ${section}`);
        setActiveSection(activeSection === section ? null : section);
    };

    // Function để handle navigation
    const handleRegisterClick = () => {
        console.log('🚀 Navigating to court owner register...');
        navigate('/court-owner-register');
    };

    const handleConsultClick = () => {
        console.log('📞 Contact consultation clicked');
        // Có thể scroll đến contact section hoặc mở modal
        document.querySelector('.contact-section')?.scrollIntoView({
            behavior: 'smooth'
        });
    };

    const benefits = [
        {
            icon: <DollarSign className="benefit-icon-svg" />,
            title: "Thu nhập ổn định",
            description: "Tăng doanh thu từ sân của bạn với hệ thống đặt online hiệu quả",
            highlight: "Tăng 40% doanh thu",
            colorClass: "emerald-teal"
        },
        {
            icon: <Users className="benefit-icon-svg" />,
            title: "Quản lý khách hàng",
            description: "Hệ thống quản lý booking và khách hàng chuyên nghiệp",
            highlight: "10,000+ khách hàng",
            colorClass: "blue-indigo"
        },
        {
            icon: <Shield className="benefit-icon-svg" />,
            title: "Bảo mật thanh toán",
            description: "Giao dịch an toàn với nhiều phương thức thanh toán",
            highlight: "100% bảo mật",
            colorClass: "purple-pink"
        },
        {
            icon: <Clock className="benefit-icon-svg" />,
            title: "Quản lý 24/7",
            description: "Hệ thống hoạt động liên tục, không giới hạn thời gian",
            highlight: "Hỗ trợ 24/7",
            colorClass: "orange-red"
        }
    ];

    const features = [
        { icon: <Zap className="feature-icon" />, text: "Bảo mật cực tốt" },
        { icon: <Heart className="feature-icon" />, text: "Hoa hồng chỉ từ 5%" },
        { icon: <Target className="feature-icon" />, text: "Hỗ trợ 24/7" },
    ];

    const stats = [
        { number: "500+", label: "Sân đối tác", icon: <MapPin className="stat-icon" /> },
        { number: "10,000+", label: "Booking mỗi tháng", icon: <TrendingUp className="stat-icon" /> },
        { number: "98%", label: "Độ hài lòng", icon: <Star className="stat-icon" /> }
    ];

    const policyItems = [
        {
            title: "1. Điều kiện tham gia",
            icon: <CheckCircle className="policy-icon" />,
            content: [
                "Sân thể thao phải đảm bảo các tiêu chuẩn an toàn và chất lượng cơ bản",
                "Cung cấp đầy đủ thông tin về địa chỉ, liên hệ và mô tả sân",
                "Cam kết tuân thủ các quy định của nền tảng"
            ]
        },
        {
            title: "2. Quyền lợi của chủ sân",
            icon: <Gift className="policy-icon" />,
            content: [
                "Được hỗ trợ quảng bá sân trên nền tảng với nhiều người dùng",
                "Sử dụng miễn phí hệ thống quản lý booking và lịch đặt sân",
                "Nhận thanh toán nhanh chóng qua các kênh thanh toán trực tuyến",
                "Được hỗ trợ kỹ thuật 24/7 từ đội ngũ chăm sóc khách hàng",
                "Tham gia các chương trình khuyến mãi và sự kiện đặc biệt"
            ]
        },
        {
            title: "3. Nghĩa vụ của chủ sân",
            icon: <Shield className="policy-icon" />,
            content: [
                "Cung cấp thông tin chính xác về sân, giá cả và dịch vụ",
                "Đảm bảo chất lượng sân và dịch vụ như cam kết",
                "Phản hồi và xử lý booking trong thời gian quy định (tối đa 2 giờ)",
                "Tuân thủ giá cả đã niêm yết trên hệ thống",
                "Báo cáo kịp thời các vấn đề phát sinh với khách hàng"
            ]
        },
        {
            title: "4. Chính sách hoa hồng",
            icon: <DollarSign className="policy-icon" />,
            content: [
                "Nền tảng thu phí dịch vụ 8-12% trên mỗi giao dịch thành công",
                "Miễn phí 3 tháng đầu cho chủ sân mới tham gia",
                "Chủ sân có thể rút tiền hàng tuần vào thứ 2 và thứ 6",
                "Không có phí ẩn, minh bạch trong tất cả giao dịch",
                "Ưu đãi giảm phí cho các sân có doanh thu cao"
            ]
        },
        {
            title: "5. Chính sách hủy và hoàn tiền",
            icon: <Clock className="policy-icon" />,
            content: [
                "Khách hàng có thể hủy booking trước 2 giờ mà không mất phí",
                "Hủy trong vòng 2 giờ sẽ mất 50% phí đặt sân",
                "Chủ sân có quyền hủy trong trường hợp bất khả kháng",
                "Hoàn tiền 100% nếu sân có vấn đề kỹ thuật không thể sử dụng",
                "Xử lý hoàn tiền trong vòng 1-3 ngày làm việc"
            ]
        },
        {
            title: "6. Tiêu chuẩn chất lượng",
            icon: <Award className="policy-icon" />,
            content: [
                "Sân phải được bảo trì thường xuyên và sạch sẽ",
                "Đảm bảo đầy đủ trang thiết bị cơ bản (lưới, trụ, bóng...)",
                "Có khu vực thay đồ và vệ sinh phù hợp",
                "Đảm bảo an ninh và an toàn cho người chơi",
                "Có nhân viên hỗ trợ trong giờ hoạt động"
            ]
        }
    ];

    console.log('🎨 Rendering component with isVisible:', isVisible);

    return (
        <div
            className="court-owner-policy"
            style={{
                minHeight: '100vh',
                position: 'relative',
                zIndex: 1
            }}
        >
            {/* Debug Corner */}

            {/* Header */}
            <div className="hero-section">
                <div className="hero-background">
                    <div className="floating-element floating-1"></div>
                    <div className="floating-element floating-2"></div>
                    <div className="floating-element floating-3"></div>
                    <div className="floating-element floating-4"></div>
                </div>

                <div className="container">
                    <div className="hero-content animate-in" id="hero">
                        <div className="hero-badge">
                            <Star className="badge-icon" />
                            <span>Đối tác tin cậy #1 Việt Nam</span>
                        </div>

                        <h1 className="hero-title">
                            Trở Thành{' '}
                            <span className="gradient-text">Đối Tác</span>
                            <br />
                            Chủ Sân
                        </h1>

                        <p className="hero-subtitle">
                            Gia nhập mạng lưới sân thể thao hàng đầu và tận hưởng thu nhập ổn định với hàng ngàn khách hàng tiềm năng
                        </p>

                        <div className="hero-features">
                            {features.map((feature, index) => (
                                <div key={index} className="feature-badge">
                                    <div className="feature-icon-wrapper">
                                        {feature.icon}
                                    </div>
                                    <span>{feature.text}</span>
                                </div>
                            ))}
                        </div>

                        <div className="hero-buttons">
                            <button className="btn-primary" onClick={handleRegisterClick}>
                                Đăng Ký Ngay
                                <ArrowRight className="btn-arrow" />
                            </button>

                            <button className="btn-secondary" onClick={handleConsultClick}>
                                Tư Vấn Miễn Phí
                            </button>
                        </div>
                    </div>
                </div>
            </div>

            {/* Stats Section */}
            <div className="stats-section">
                <div className="container">
                    <div className="stats-grid animate-in" id="stats">
                        {stats.map((stat, index) => (
                            <div key={index} className="stat-card">
                                <div className="stat-icon-wrapper">
                                    {stat.icon}
                                </div>
                                <div className="stat-number">{stat.number}</div>
                                <div className="stat-label">{stat.label}</div>
                            </div>
                        ))}
                    </div>
                </div>
            </div>

            {/* Benefits Section */}
            <div className="benefits-section">
                <div className="container">
                    <div className="section-header animate-in" id="benefits-title">
                        <h2 className="section-title">Tại Sao Chọn Chúng Tôi?</h2>
                        <p className="section-subtitle">
                            Những lợi ích vượt trội khi trở thành đối tác của chúng tôi
                        </p>
                    </div>

                    <div className="benefits-grid animate-in" id="benefits-grid">
                        {benefits.map((benefit, index) => (
                            <div key={index} className={`benefit-card ${benefit.colorClass}`}>
                                <div className="benefit-overlay"></div>

                                <div className="benefit-icon-wrapper">
                                    {benefit.icon}
                                </div>

                                <div className="benefit-highlight">
                                    {benefit.highlight}
                                </div>

                                <h3 className="benefit-title">
                                    {benefit.title}
                                </h3>

                                <p className="benefit-description">
                                    {benefit.description}
                                </p>

                            </div>
                        ))}
                    </div>
                </div>
            </div>

            {/* Policy Section */}
            <div className="policy-section">
                <div className="container">
                    <div className="section-header animate-in" id="policy-title">
                        <h2 className="section-title">Điều Khoản & Chính Sách</h2>
                        <p className="section-subtitle">
                            Minh bạch, rõ ràng và có lợi cho đối tác
                        </p>
                    </div>

                    <div className="policy-items animate-in" id="policy-items">
                        {policyItems.map((item, index) => (
                            <div key={index} className="policy-item">
                                <button
                                    onClick={() => toggleSection(index)}
                                    className="policy-header"
                                >
                                    <div className="policy-header-content">
                                        <div className="policy-icon-wrapper">
                                            {item.icon}
                                        </div>
                                        <h3 className="policy-title">{item.title}</h3>
                                    </div>
                                    <ChevronDown
                                        className={`policy-chevron ${activeSection === index ? 'active' : ''}`}
                                    />
                                </button>

                                <div className={`policy-content ${activeSection === index ? 'active' : ''}`}>
                                    <div className="policy-content-inner">
                                        <ul className="policy-list">
                                            {item.content.map((point, pointIndex) => (
                                                <li key={pointIndex} className="policy-list-item">
                                                    <div className="policy-check">
                                                        <CheckCircle className="check-icon" />
                                                    </div>
                                                    <span>{point}</span>
                                                </li>
                                            ))}
                                        </ul>
                                    </div>
                                </div>
                            </div>
                        ))}
                    </div>
                </div>
            </div>

            {/* CTA Section */}
            <div className="cta-section">
                <div className="cta-background"></div>

                <div className="container">
                    <div className="cta-content animate-in" id="cta">
                        <div className="cta-icon-wrapper">
                            <Award className="cta-icon" />
                        </div>

                        <h2 className="cta-title">
                            Sẵn Sàng Bắt Đầu Hành Trình?
                        </h2>

                        <p className="cta-subtitle">
                            Gia nhập ngay hôm nay để tăng doanh thu và tiếp cận hàng ngàn khách hàng tiềm năng
                        </p>
                        <div className="cta-buttons">
                            {/* Button chính sẽ navigate đến /court-owner-register */}
                            <button className="cta-btn-primary" onClick={handleRegisterClick}>
                                <div className="cta-btn-icon">
                                    <Play className="play-icon" />
                                </div>
                                Đăng Ký Chủ Sân Ngay
                                <ArrowRight className="cta-btn-arrow" />
                            </button>

                            <div className="contact-info">
                                <Phone className="contact-icon" />
                                <div className="contact-details">
                                    <div className="contact-label">Hotline 24/7</div>
                                    <div className="contact-value">1900-xxxx</div>
                                </div>
                            </div>
                        </div>

                        <div className="cta-features">
                            <span>✅ Có thể hủy bất cứ lúc nào</span>
                            <span>✅ Không cam kết dài hạn</span>
                            <span>✅ Miễn phí thiết lập</span>
                        </div>
                    </div>
                </div>
            </div>

            {/* Contact Section */}
            <div className="contact-section">
                <div className="container">
                    <h3 className="contact-title">Liên Hệ Với Chúng Tôi</h3>
                    <div className="contact-grid">
                        <div className="contact-item">
                            <div className="contact-item-icon phone">
                                <Phone className="contact-svg" />
                            </div>
                            <div className="contact-item-content">
                                <div className="contact-item-label">Hotline</div>
                                <div className="contact-item-value">1900-xxxx</div>
                            </div>
                        </div>

                        <div className="contact-item">
                            <div className="contact-item-icon email">
                                <Mail className="contact-svg" />
                            </div>
                            <div className="contact-item-content">
                                <div className="contact-item-label">Email</div>
                                <div className="contact-item-value">support@example.com</div>
                            </div>
                        </div>

                        <div className="contact-item">
                            <div className="contact-item-icon address">
                                <MapPin className="contact-svg" />
                            </div>
                            <div className="contact-item-content">
                                <div className="contact-item-label">Địa chỉ</div>
                                <div className="contact-item-value">Hà Nội, Việt Nam</div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default CourtOwnerPolicy;