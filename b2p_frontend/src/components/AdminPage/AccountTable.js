import React, { useState, useEffect } from "react";
import "./AccountTable.scss";
import { UserOutlined } from "@ant-design/icons";
import {
    Table,
    Input,
    Select,
    Modal,
    message,
    Tooltip,
    Typography,
} from "antd";
import {
    SearchOutlined,
    LockOutlined,
    UnlockOutlined,
    DeleteOutlined,
    SettingOutlined,
    ExclamationCircleOutlined,
} from "@ant-design/icons";
import {
    getAccountList,
    deleteUser,
    banUser,
    unbanUser,
} from "../../services/apiService";

const { Option } = Select;
const { Text } = Typography;

const ROLE_MAP = {
    Owner: 3,
    Player: 2,
};

const STATUS_MAP = {
    Active: 1,
    Banned: 4,
};

const ModalContent = ({ title, content, type, tagLabel }) => (
    <div className="custom-modal-content">
        <div className="modal-title-row">
            <ExclamationCircleOutlined className="modal-icon" />
            <span className="modal-title">{title}</span>
        </div>
        <div className="modal-body">
            <p>{content}</p>
            {tagLabel && (
                <div className={`modal-tag-box ${type}`}>
                    {type === "ban" && <LockOutlined />}
                    {type === "unban" && <UnlockOutlined />}
                    {type === "delete" && <DeleteOutlined />}
                    <span>{tagLabel}</span>
                </div>
            )}
            <div className="modal-warning-box">
                <ExclamationCircleOutlined />
                <span>Hành động này không thể hoàn tác!</span>
            </div>
        </div>
    </div>
);

const AccountTable = () => {
    const [accounts, setAccounts] = useState([]);
    const [loading, setLoading] = useState(false);
    const [searchText, setSearchText] = useState("");
    const [roleFilter, setRoleFilter] = useState(null);
    const [statusFilter, setStatusFilter] = useState(null);
    const [pagination, setPagination] = useState({
        current: 1,
        pageSize: 5,
        total: 0,
        showSizeChanger: true,
    });

    const fetchAccounts = async () => {
        try {
            setLoading(true);
            const requestData = {
                search: searchText || "",
                roleId: ROLE_MAP[roleFilter] || null,
                statusId: STATUS_MAP[statusFilter] || null,
                pageNumber: pagination.current,
                pageSize: pagination.pageSize,
            };

            const response = await getAccountList(requestData);
            if (response?.data) {
                setAccounts(Array.isArray(response.data.items) ? response.data.items : []);
                setPagination((prev) => ({
                    ...prev,
                    total: response.data.totalItems || 0,
                }));
            } else {
                setAccounts([]);
                setPagination((prev) => ({ ...prev, total: 0 }));
            }
        } catch (error) {
            message.error("Có lỗi xảy ra khi tải danh sách tài khoản");
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchAccounts();
    }, [searchText, roleFilter, statusFilter, pagination.current, pagination.pageSize]);

    const handleSearch = (value) => {
        setSearchText(value);
        setPagination((prev) => ({ ...prev, current: 1 }));
    };

    const handleTableChange = (newPagination) => {
        setPagination({
            ...pagination,
            current: newPagination.current,
            pageSize: newPagination.pageSize,
        });
    };

    const handleRoleFilter = (value) => {
        setRoleFilter(value);
        setPagination((prev) => ({ ...prev, current: 1 }));
    };

    const handleStatusFilter = (value) => {
        setStatusFilter(value);
        setPagination((prev) => ({ ...prev, current: 1 }));
    };

    const handleToggleStatus = async (record) => {
        const isBanned = record.statusName === "Banned";
        Modal.confirm({
            icon: null,
            centered: true,
            content: (
                <ModalContent
                    title={`Xác nhận ${isBanned ? "mở khóa" : "khóa"} tài khoản`}
                    content={`Bạn có chắc chắn muốn ${isBanned ? "mở khóa" : "khóa"} tài khoản này không?`}
                    type={isBanned ? "unban" : "ban"}
                    tagLabel={record.fullName}
                />
            ),
            okText: isBanned ? "Xác nhận mở khóa" : "Xác nhận khóa",
            cancelText: "Hủy bỏ",
            okButtonProps: { className: "confirm-ok-btn" },
            cancelButtonProps: { className: "confirm-cancel-btn" },
            onOk: async () => {
                try {
                    setLoading(true);
                    const res = isBanned
                        ? await unbanUser(record.userId)
                        : await banUser(record.userId);
                    if (res?.status === 200) {
                        message.success(`${isBanned ? "Mở khóa" : "Khóa"} thành công`);
                        fetchAccounts();
                    }
                } catch (e) {
                    message.error("Lỗi khi cập nhật trạng thái");
                } finally {
                    setLoading(false);
                }
            },
        });
    };

    const handleDelete = (record) => {
        if (record.statusName === "Active") {
            Modal.warning({
                centered: true,
                title: "Không thể xóa tài khoản",
                content: (
                    <>
                        <p>
                            Tài khoản <strong>{record.fullName}</strong> hiện đang hoạt động.
                        </p>
                        <p>Vui lòng khóa tài khoản trước khi thực hiện thao tác xóa.</p>
                    </>
                ),
                okText: "Đã hiểu",
            });
            return;
        }

        Modal.confirm({
            icon: null,
            centered: true,
            content: (
                <ModalContent
                    title="Xác nhận xóa tài khoản"
                    content="Bạn có chắc chắn muốn xóa tài khoản này không?"
                    type="delete"
                    tagLabel={record.fullName}
                />
            ),
            okText: "Xác nhận xóa",
            cancelText: "Hủy bỏ",
            okButtonProps: { className: "confirm-ok-btn" },
            cancelButtonProps: { className: "confirm-cancel-btn" },
            onOk: async () => {
                try {
                    const res = await deleteUser(record.userId);
                    if (res?.status === 200) {
                        message.success("Xóa thành công");
                        fetchAccounts();
                    }
                } catch (e) {
                    message.error("Lỗi khi xóa tài khoản");
                }
            },
        });
    };

    const columns = [
        {
            title: "STT",
            key: "index",
            render: (_, __, index) => (
                <strong style={{ color: "#52c41a" }}>
                    {(pagination.current - 1) * pagination.pageSize + index + 1}
                </strong>
            ),
            align: "center",
        },
        {
            title: "UserID",
            dataIndex: "userId",
            key: "userId",
            align: "center",
        },
        {
            title: "Họ và tên",
            dataIndex: "fullName",
            key: "fullName",
            align: "center",
        },
        {
            title: "Email",
            dataIndex: "email",
            key: "email",
            align: "center",
        },
        {
            title: "Số điện thoại",
            dataIndex: "phone",
            key: "phone",
            align: "center",
        },
        {
            title: "Loại tài khoản",
            dataIndex: "roleName",
            key: "roleName",
            align: "center",
        },
        {
            title: "Trạng thái",
            key: "status",
            align: "center",
            render: (_, record) => (
                <div className="status-icon" onClick={() => handleToggleStatus(record)}>
                    {record.statusName === "Banned" ? (
                        <Tooltip title="Bị khóa">
                            <LockOutlined className="anticon-lock" />
                        </Tooltip>
                    ) : (
                        <Tooltip title="Hoạt động">
                            <UnlockOutlined className="anticon-unlock" />
                        </Tooltip>
                    )}
                </div>
            ),
        },
        {
            title: "Hành động",
            key: "actions",
            align: "center",
            render: (_, record) => (
                <div className="action-buttons">
                    <Tooltip title="Xóa">
                        <DeleteOutlined className="delete-icon" onClick={() => handleDelete(record)} />
                    </Tooltip>
                    <Tooltip title="Cài đặt">
                        <SettingOutlined className="setting-icon" />
                    </Tooltip>
                </div>
            ),
        },
    ];

    return (
        <div className="account-table-wrapper">
            <div className="account-table-card">
                <div className="page-header">
                    <h1>
                        <UserOutlined style={{ fontSize: "22px", marginRight: "8px", color: "#ffffff" }} />
                        Quản lý tài khoản
                    </h1>

                    <Text type="secondary">
                        {searchText ? (
                            <>
                                Tìm thấy <Text strong>{pagination.total}</Text> kết quả cho "{searchText}"
                            </>
                        ) : (
                            <>
                                Tổng cộng <Text strong>{pagination.total}</Text> tài khoản
                            </>
                        )}
                    </Text>
                </div>

                <div className="table-filters">
                    <Input.Search
                        placeholder="Tìm kiếm theo tên, id, email"
                        prefix={<SearchOutlined />}
                        className="search-input"
                        allowClear
                        enterButton={<SearchOutlined />}
                        value={searchText}
                        onChange={(e) => handleSearch(e.target.value)}
                        onSearch={handleSearch}
                    />

                    <div className="filter-section">
                        <Select
                            placeholder="Loại tài khoản"
                            style={{ width: 160 }}
                            allowClear
                            onChange={handleRoleFilter}
                            value={roleFilter}
                        >
                            <Option value={null}>Tất cả</Option>
                            <Option value="Owner">Chủ sân</Option>
                            <Option value="Player">Người chơi</Option>
                        </Select>

                        <Select
                            placeholder="Trạng thái"
                            style={{ width: 160 }}
                            allowClear
                            onChange={handleStatusFilter}
                            value={statusFilter}
                        >
                            <Option value={null}>Tất cả</Option>
                            <Option value="Active">Hoạt động</Option>
                            <Option value="Banned">Bị khóa</Option>
                        </Select>
                    </div>
                </div>

                <Table
                    columns={columns}
                    dataSource={accounts}
                    loading={loading}
                    pagination={{
                        ...pagination,
                        pageSizeOptions: ["5", "10", "20"],
                    }}
                    onChange={handleTableChange}
                    rowKey="userId"
                    locale={{
                        emptyText: loading
                            ? "Đang tải dữ liệu..."
                            : searchText || roleFilter || statusFilter
                                ? "Không tìm thấy kết quả phù hợp"
                                : "Không có dữ liệu tài khoản",
                    }}
                />
            </div>
        </div>
    );
};

export default AccountTable;
