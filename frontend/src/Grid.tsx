import { GridGenerator, HexGrid, Layout, Hexagon } from "react-hexgrid";
import "./Grid.css";

function Grid() {
  const hexagonSize = { x: 3, y: 3 };
  const hexagons = GridGenerator.hexagon(10);
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
                className="hexi"
                key={index}
                q={hex.q}
                r={hex.r}
                s={hex.s}
              />
            ))}
          </Layout>
        </HexGrid>
      </div>
    </>
  );
}

export default Grid;
