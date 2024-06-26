import { useState, useEffect } from "react";
import { GridGenerator, HexGrid, Layout, Hexagon, Hex } from "react-hexgrid";
import { HubConnectionBuilder } from "@microsoft/signalr";
import "./Grid.css";
import { FaRobot } from "react-icons/fa";

const gridSize = 10;
const outterColors = ["#b0b0b0", "#2c802e", "#932929", "#273586"];
const playerColors = ["#b0b0b0", "#38a43a", "#b32e2e", "#4a5fd6"];
const hexagonSize = { x: 3, y: 3 };
const outerHexagons = GridGenerator.ring({ q: 0, r: 0, s: 0 }, gridSize);

interface HexagonData {
  R: number;
  S: number;
  Q: number;
  Index: number;
  Player: number;
}
interface GameState {
  Hexagons: HexagonData[];
  FreeColors: number[];
}

const initialGameState: GameState = {
  Hexagons: [],
  FreeColors: [],
};

function Grid() {
  const [gameState, setGameState] = useState<GameState>(initialGameState);
  const [IAM, setIAM] = useState(0);
  const [connection, setConnection] = useState<any>(null); // Store the connection

  useEffect(() => {
    const hubConnection = new HubConnectionBuilder()
      .withUrl("http://localhost:5012/websockets")
      .withAutomaticReconnect()
      .build();

    hubConnection
      .start()
      .then(() => {
        console.log("SignalR connection established");
        hubConnection.invoke("GetState");
        setConnection(hubConnection); // Store the connection once it's established
      })
      .catch((err) => {
        console.error(err.toString());
      });

    hubConnection.on("GetState", (message) => {
      try {
        const jsonMessage = JSON.parse(message);
        setGameState(jsonMessage);
      } catch (error) {
        console.error("Error parsing JSON:", error);
      }
    });

    return () => {
      // Clean up the connection when the component unmounts
      if (connection) {
        connection.stop();
      }
    };
  }, []);

  const SelectColor = (playerNum: number) => {
    setIAM(playerNum);
    const connection = new HubConnectionBuilder()
      .withUrl("http://localhost:5012/websockets")
      .withAutomaticReconnect()
      .build();
    connection
      .start()
      .then(() => {
        connection.invoke("SelectColor", playerNum);
      })
      .catch((err) => {
        console.error(err);
      });
  };

  const FillWithAiPlayers = () => {
    const connection = new HubConnectionBuilder()
      .withUrl("http://localhost:5012/websockets")
      .withAutomaticReconnect()
      .build();
    connection
      .start()
      .then(() => {
        connection.invoke("FillWithAiPlayers");
      })
      .catch((err) => {
        console.error(err);
      });
  };

  const SendMove = (hex: HexagonData) => {
    const connection = new HubConnectionBuilder()
      .withUrl("http://localhost:5012/websockets")
      .withAutomaticReconnect()
      .build();
    hex.Player = IAM;
    connection
      .start()
      .then(() => {
        console.log("SignalR connection established");
        connection.invoke("Move", JSON.stringify(hex));
      })
      .catch((err) => {
        console.error(err);
      });
  };

  const getPlayerColor = (hex: Hex) => {
    if (
      (Math.abs(hex.q) === gridSize && Math.abs(hex.r) === gridSize) ||
      (Math.abs(hex.s) === gridSize && Math.abs(hex.r) === gridSize) ||
      (Math.abs(hex.q) === gridSize && Math.abs(hex.s) === gridSize)
    ) {
      return "#808080";
    }
    if (Math.abs(hex.q) === gridSize) return outterColors[1];
    if (Math.abs(hex.r) === gridSize) return outterColors[2];
    if (Math.abs(hex.s) === gridSize) return outterColors[3];
    return "#b0b0b0";
  };

  return (
    <>
      <div className="flex justify-center">
        {gameState.FreeColors.length > 0 && (
          <>
            {[1, 2, 3].map((colorNum) => (
              <button
                key={colorNum}
                className="flex m-1"
                style={{ backgroundColor: playerColors[colorNum] }}
                onClick={() => SelectColor(colorNum)}
                disabled={!gameState.FreeColors.includes(colorNum)}
              >
                Player {colorNum}
              </button>
            ))}
          </>
        )}

        {gameState.FreeColors.length > 0 && (
          <>
            <button
              className="w-max-[100px] m-1 bg-gray-400 flex items-center"
              onClick={() => FillWithAiPlayers()}
            >
              <FaRobot className="w-6 pr-1" />{" "}
              {/* Added margin to separate icon from text */}
              Fill with AI
            </button>
          </>
        )}
      </div>

      <div className="pt-[3em] h-[100%] w-max-[100vw] h-max-[100vw]">
        <HexGrid className="h-[80%]">
          <Layout
            size={hexagonSize}
            flat={false}
            spacing={1.05}
            origin={{ x: 0, y: 0 }}
          >
            {gameState.Hexagons.map((hex, index) => (
              <Hexagon
                key={index}
                q={hex.Q}
                r={hex.R}
                s={hex.S}
                style={{ fill: playerColors[hex.Player] }}
                onClick={() => IAM !== 0 && SendMove(hex)} // Call sendMessage with hex object when clicked
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
