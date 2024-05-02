import { GridGenerator, HexGrid, Layout, Hexagon, Hex } from "react-hexgrid";
import "./Grid.css";

function Grid() {
  const gridSize = 10;
  const hexagonSize = { x: 3, y: 3 };
  const hexagons = GridGenerator.hexagon(gridSize);
  const outerHexagons = GridGenerator.ring({ q: 0, r: 0, s: 0 }, 10);

  const playerColors = ["#3ebb40", "#c03030", "#2f43b0"]; // Define colors for each player

  const getPlayerColor = (hex: Hex) => {
    // Check if the hexagon is on the edge and assign color accordingly
    if (
      (Math.abs(hex.q) === gridSize && Math.abs(hex.r) === gridSize) ||
      (Math.abs(hex.s) === gridSize && Math.abs(hex.r) === gridSize) ||
      (Math.abs(hex.q) === gridSize && Math.abs(hex.s) === gridSize)
    ) {
      return "#808080"; // Gray color for edges and corners
    }
    if (Math.abs(hex.q) === gridSize) return playerColors[0]; // Player 1
    if (Math.abs(hex.r) === gridSize) return playerColors[1]; // Player 2
    if (Math.abs(hex.s) === gridSize) return playerColors[2]; // Player 3
    return "#b0b0b0"; // Default color for inner hexagons
  };

  return (
    <>
      <div className="grid-wrapper">
        <HexGrid>
          <Layout
            size={hexagonSize}
            flat={false}
            spacing={1.05}
            origin={{ x: 0, y: 0 }}
          >
            {hexagons.map((hex, index) => (
              <Hexagon
                id="inner-hexagon"
                key={index}
                q={hex.q}
                r={hex.r}
                s={hex.s}
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
                id="outter-hexagon"
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
