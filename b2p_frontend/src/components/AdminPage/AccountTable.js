import React, { useState, useEffect } from "react";
import "./AccountTable.scss";
import { Table, Input, Select, Modal, message } from "antd";
import {
    SearchOutlined,
    LockOutlined,
    UnlockOutlined,
    DeleteOutlined,
    SettingOutlined,
} from "@ant-design/icons";
import {
    getAccountList,
    deleteUser,
    banUser,
    unbanUser,
} from "../../services/apiService";

const { Option } = Select;

const AccountTable = () => {
    const [accounts, setAccounts] = useState([]);
    const [loading, setLoading] = useState(false);
    const [searchText, setSearchText] = useState("");
    const [roleFilter, setRoleFilter] = useState(null);
    const [statusFilter, setStatusFilter] = useState(null);
    const [pagination, setPagination] = useState({
        current: 1,
        pageSize: 10,
        total: 0,
        showSizeChanger: true,
    });

    const fetchAccounts = async () => {
        try {
            setLoading(true);
            const requestData = {
                search: searchText || "",
                roleType: roleFilter || "",
                status: statusFilter || "",
                pageNumber: pagination.current,
                pageSize: pagination.pageSize,
            };

            const response = await getAccountList(requestData);
            if (response?.data) {
                setAccounts(response.data.items || []);
                setPagination((prev) => ({
                    ...prev,
                    total: response.data.totalItems || 0,
                }));
            }
        } catch (error) {
            message.error("Có lỗi xảy ra khi tải danh sách tài khoản");
            console.error("Error:", error);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchAccounts();
    }, [searchText, roleFilter, statusFilter, pagination.current, pagination.pageSize]);

    const handleTableChange = (newPagination) => {
        setPagination((prev) => ({
            ...prev,
            current: newPagination.current,
            pageSize: newPagination.pageSize,
        }));
    };

    const handleSearch = (value) => {
        setSearchText(value);
        setPagination((prev) => ({ ...prev, current: 1 }));
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
        try {
            setLoading(true);
            if (record.statusName === "Banned") {
                const response = await unbanUser(record.userId);
                if (response?.status === 200) {
                    message.success("Mở khóa tài khoản thành công");
                    await fetchAccounts();
                }
            } else {
                const response = await banUser(record.userId);
                if (response?.status === 200) {
                    message.success("Khóa tài khoản thành công");
                    await fetchAccounts();
                }
            }
        } catch (error) {
            message.error("Có lỗi xảy ra khi thay đổi trạng thái tài khoản");
            console.error("Error:", error);
        } finally {
            setLoading(false);
        }
    };

    const handleDelete = async (record) => {
        Modal.confirm({
            title: "Xác nhận xóa tài khoản",
            content: `Bạn có chắc chắn muốn xóa tài khoản "${record.fullName}"?`,
            okText: "Xóa",
            okType: "danger",
            cancelText: "Hủy",
            onOk: async () => {
                try {
                    const response = await deleteUser(record.userId);
                    if (response?.status === 200) {
                        message.success("Xóa tài khoản thành công");
                        await fetchAccounts();
                    }
                } catch (error) {
                    message.error("Có lỗi xảy ra khi xóa tài khoản");
                    console.error("Error:", error);
                }
            },
        });
    };

    const columns = [
        {
            title: "ID",
            dataIndex: "userId",
            key: "userId",
            width: "5%",
            align: "center",
        },
        {
            title: "Họ và tên",
            dataIndex: "fullName",
            key: "fullName",
            width: "20%",
            align: "center",
        },
        {
            title: "Email",
            dataIndex: "email",
            key: "email",
            width: "20%",
            align: "center",
        },
        {
            title: "Số điện thoại",
            dataIndex: "phone",
            key: "phone",
            width: "15%",
            align: "center",
        },
        {
            title: "Loại tài khoản",
            dataIndex: "roleName",
            key: "roleName",
            width: "15%",
            align: "center",
        },
        {
            title: "Trạng thái",
            key: "status",
            width: "10%",
            align: "center",
            render: (_, record) => (
                <div
                    className="status-icon"
                    onClick={() => handleToggleStatus(record)}
                    style={{ cursor: "pointer" }}
                >
                    {record.statusName === "Banned" ? (
                        <LockOutlined
                            className="anticon-lock"
                            style={{ color: "#ff4d4f", fontSize: "18px" }}
                        />
                    ) : (
                        <UnlockOutlined
                            className="anticon-unlock"
                            style={{ color: "#52c41a", fontSize: "18px" }}
                        />
                    )}
                </div>
            ),
        },
        {
            title: "Hành động",
            key: "actions",
            width: "15%",
            align: "center",
            render: (_, record) => (
                <div className="action-buttons">
                    <DeleteOutlined
                        className="delete-icon"
                        onClick={() => handleDelete(record)}
                    />
                    <SettingOutlined
                        className="setting-icon"
                        onClick={() => {
                            // xử lý setting nếu cần
                        }}
                    />
                </div>
            ),
        },
    ];

    return (
        <div className="account-table">
            <h1>Quản lý tài khoản</h1>

            <div className="table-filters">
                <div className="search-section">
                    <Input
                        placeholder="Tìm kiếm theo tên, id, email"
                        prefix={<SearchOutlined />}
                        className="search-input"
                        onChange={(e) => handleSearch(e.target.value)}
                        allowClear
                    />
                </div>

                <div className="filter-section">
                    <Select
                        placeholder="Loại tài khoản"
                        style={{ width: 160 }}
                        allowClear
                        onChange={handleRoleFilter}
                        value={roleFilter}
                    >
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
                        <Option value="Active">Hoạt động</Option>
                        <Option value="Banned">Bị khóa</Option>
                    </Select>
                </div>
            </div>

            <div className="table-container">
                <Table
                    columns={columns}
                    dataSource={accounts}
                    loading={loading}
                    pagination={pagination}
                    onChange={handleTableChange}
                    rowKey="userId"
                />
            </div>
        </div>
    );
};

export default AccountTable;
