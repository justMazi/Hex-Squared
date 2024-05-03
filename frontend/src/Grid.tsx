import { useState } from "react";
import { GridGenerator, HexGrid, Layout, Hexagon, Hex } from "react-hexgrid";
import "./Grid.css";

const gridSize = 10;
const playerColors = ["#3ebb40", "#c03030", "#2f43b0"];
const hexagonSize = { x: 3, y: 3 };
const hexagons = GridGenerator.hexagon(gridSize);
const outerHexagons = GridGenerator.ring({ q: 0, r: 0, s: 0 }, gridSize);

function Grid() {
  const [innerHexagons, setInnerHexagons] = useState(
    hexagons.map((hex) => ({ hex, color: "#b0b0b0" }))
  );

  const getPlayerColor = (hex: Hex) => {
    if (
      (Math.abs(hex.q) === gridSize && Math.abs(hex.r) === gridSize) ||
      (Math.abs(hex.s) === gridSize && Math.abs(hex.r) === gridSize) ||
      (Math.abs(hex.q) === gridSize && Math.abs(hex.s) === gridSize)
    ) {
      return "#808080";
    }
    if (Math.abs(hex.q) === gridSize) return playerColors[0];
    if (Math.abs(hex.r) === gridSize) return playerColors[1];
    if (Math.abs(hex.s) === gridSize) return playerColors[2];
    return "#b0b0b0";
  };

  const handleHexClick = (hex: Hex) => {
    const updatedHexagons = innerHexagons.map((item) =>
      item.hex.q === hex.q && item.hex.r === hex.r && item.hex.s === hex.s
        ? { ...item, color: "#ff0000" }
        : item
    );
    setInnerHexagons(updatedHexagons);
  };

  return (
    <>
      <div className="pt-[3em] h-[100%] w-max-[100vw] h-max-[100vw]">
        <HexGrid className="h-[80%]">
          <Layout
            size={hexagonSize}
            flat={false}
            spacing={1.05}
            origin={{ x: 0, y: 0 }}
          >
            {innerHexagons.map((item, index) => (
              <Hexagon
                className="hover:fill-rose-700 fill-[#4e2020]"
                key={index}
                q={item.hex.q}
                r={item.hex.r}
                s={item.hex.s}
                onClick={() => handleHexClick(item.hex)}
              />
            ))}
          </Layout>
          <Layout
            size={hexagonSize}
            flat={false}
            spacing={1.05}
            origin={{ x: 0, y: 0 }}
          >
            {outerHexagons.map((hex, index) => (
              <Hexagon
                key={index}
                q={hex.q}
                r={hex.r}
                s={hex.s}
                style={{ fill: getPlayerColor(hex) }}
              />
            ))}
          </Layout>
        </HexGrid>
      </div>
    </>
  );
}

export default Grid;
