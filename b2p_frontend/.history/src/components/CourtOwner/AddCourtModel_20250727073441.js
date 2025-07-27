import React, { useState } from 'react';
import { Modal, Button, Form, Row, Col } from 'react-bootstrap';

const AddCourtModal = ({ show, handleClose, categories, facilityId }) => {
  const [formData, setFormData] = useState({
    courtName: '',
    categoryId: '',
    pricePerHour: ''
  });

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value
    }));
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    const submitData = {
      ...formData,
      facilityId: facilityId,
      categoryId: parseInt(formData.categoryId)
    };
    console.log('Data to submit:', submitData);
    // TODO: Gọi API thêm sân mới ở đây
    handleClose();
  };

  return (
    <Modal show={show} onHide={handleClose} size="lg">
      <Modal.Header closeButton>
        <Modal.Title>Thêm Mới Sân</Modal.Title>
      </Modal.Header>
      <Form onSubmit={handleSubmit}>
        <Modal.Body>
          <Row className="mb-3">
            <Form.Group as={Col} controlId="courtName">
              <Form.Label>Tên Sân <span className="text-danger">*</span></Form.Label>
              <Form.Control
                type="text"
                name="courtName"
                value={formData.courtName}
                onChange={handleChange}
                required
              />
            </Form.Group>

            <Form.Group as={Col} controlId="categoryId">
              <Form.Label>Loại Sân <span className="text-danger">*</span></Form.Label>
              <Form.Select
                name="categoryId"
                value={formData.categoryId}
                onChange={handleChange}
                required
              >
                <option value="">Chọn loại sân</option>
                {categories.map(category => (
                  <option key={category.id} value={category.id}>
                    {category.name}
                  </option>
                ))}
              </Form.Select>
            </Form.Group>
          </Row>

          <Row className="mb-3">
            <Form.Group as={Col} controlId="pricePerHour">
              <Form.Label>Giá/Giờ (VND) <span className="text-danger">*</span></Form.Label>
              <Form.Control
                type="number"
                name="pricePerHour"
                value={formData.pricePerHour}
                onChange={handleChange}
                min="0"
                required
              />
            </Form.Group>
          </Row>
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={handleClose}>
            Hủy
          </Button>
          <Button variant="primary" type="submit">
            Thêm Mới
          </Button>
        </Modal.Footer>
      </Form>
    </Modal>
  );
};

export default AddCourtModal;