import { useState } from "react";
import Carousel from "react-bootstrap/Carousel";

const SliderField = (props) => {
  const [index, setIndex] = useState(0);

  const handleSelect = (selectedIndex) => {
    setIndex(selectedIndex);
  };

  return (
    <Carousel activeIndex={index} onSelect={handleSelect}>
      <Carousel.Item></Carousel.Item>
    </Carousel>
  );
};

export default SliderField;
