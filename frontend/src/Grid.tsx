import { GridGenerator, HexGrid, Layout, Hexagon } from "react-hexgrid";

function Grid() {
  const hexagonSize = { x: 3, y: 3 };
  const hexagons = GridGenerator.hexagon(10);
  return (
    <>
      <HexGrid>
        <Layout
          size={hexagonSize}
          flat={true}
          spacing={1.05}
          origin={{ x: 0, y: 0 }}
        >
          {hexagons.map((hex, index) => (
            <Hexagon key={index} q={hex.q} r={hex.r} s={hex.s} />
          ))}
        </Layout>
      </HexGrid>
    </>
  );
}

export default Grid;
