import { useState, useEffect } from "react";
import { GridGenerator, HexGrid, Layout, Hexagon, Hex } from "react-hexgrid";
import { HubConnectionBuilder } from "@microsoft/signalr";
import "./Grid.css";
import { FaRobot } from "react-icons/fa";
import { Copy } from "lucide-react";
import { Button } from "./components/ui/button";
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "./components/ui/dialog";
import { Input } from "./components/ui/input";
import { Label } from "./components/ui/label";
import React from "react";
import { ExternalLink } from "lucide-react";
import { get } from "http";

const gridSize = 10;
const outterColors = ["#b0b0b0", "#2c802e", "#932929", "#273586"];
const playerColors = ["#b0b0b0", "#38a43a", "#b32e2e", "#4a5fd6"];
const hexagonSize = { x: 3, y: 3 };
const outerHexagons = GridGenerator.ring({ q: 0, r: 0, s: 0 }, gridSize);
const playerNumbers = [1, 2, 3];

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
  GameCode: string;
}

const initialGameState: GameState = {
  Hexagons: [],
  FreeColors: [],
  GameCode: window.location.pathname.substring(1),
};

function Grid() {
  const [gameState, setGameState] = useState<GameState>(initialGameState);
  const [IAM, setIAM] = useState(0);
  const [isLocal, setIsLocal] = useState<boolean>(false);
  const [connection, setConnection] = useState<any>(null); // Store the connection
  const [winner, setWinner] = useState(0); // Store the connection

  useEffect(() => {
    const hubConnection = new HubConnectionBuilder()
      .withUrl("http://localhost:5012/websockets")
      .withAutomaticReconnect()
      .build();

    hubConnection
      .start()
      .then(() => {
        console.log("SignalR connection established");
        setConnection(hubConnection); // Store the connection once it's established

        // on mount set game code from the url and fetch the game
        hubConnection.invoke("GetState", gameState.GameCode);
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

    hubConnection.on("Winner", (player) => {
      try {
        setWinner(player);
        setIsWinDialogueOpen(true);
        console.log(gameState);
        console.log(player);
        console.log(IAM);
        console.log(isLocal);
        if (isLocal) {
        }
      } catch (error) {
        console.error("Error setting winner:", error);
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
        connection.invoke("SelectColor", playerNum, gameState.GameCode);
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
        connection.invoke("FillWithAiPlayers", gameState.GameCode);
      })
      .catch((err) => {
        console.error(err);
      });
  };

  const SendMove = (hex: HexagonData) => {
    if (hex.Player !== 0) return;
    const connection = new HubConnectionBuilder()
      .withUrl("http://localhost:5012/websockets")
      .withAutomaticReconnect()
      .build();
    hex.Player = IAM;
    connection
      .start()
      .then(() => {
        console.log("SignalR connection established");
        connection.invoke("Move", JSON.stringify(hex), gameState.GameCode);

        // if game is played locally, switch the player after move
        if (isLocal) {
          setIAM((IAM % 3) + 1);
        }
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

  const generateLink = () => {
    const path =
      gameState.GameCode == null || gameState.GameCode.length === 0
        ? Math.random().toString(36).substr(2, 6)
        : gameState.GameCode;
    const link = window.location.origin + "/" + path;
    return link;
  };

  const [open, setOpen] = React.useState(gameState.GameCode == "");
  const [isWinDialogueOpen, setIsWinDialogueOpen] = React.useState(false);
  const [link, _] = React.useState(generateLink());

  const copyToClipboard = () => {
    navigator.clipboard.writeText(link);
  };

  const locally = () => {
    // change url without full reload
    window.history.pushState(null, "", link);
    gameState.GameCode = link;

    playerNumbers.map((num) => {
      connection.invoke("SelectColor", num, gameState.GameCode);
    });
    setIAM(1);
    setIsLocal(true);
    setOpen(false);
    connection.invoke("GetState", gameState.GameCode);
  };

  const overInternet = () => {
    // change url without full reload
    window.history.pushState(null, "", link);
    gameState.GameCode = link;

    setIsLocal(false);
    setOpen(false);
    connection.invoke("GetState", gameState.GameCode);
  };

  return (
    <>
      <div className="flex justify-center">
        {gameState.FreeColors.length > 0 && (
          <>
            {playerNumbers.map((colorNum) => (
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
        <Dialog
          open={isWinDialogueOpen}
          onOpenChange={() => {
            window.location.href = "/";
          }}
        >
          <div
            className={`${
              isWinDialogueOpen ? "backdrop-blur-sm fixed inset-0" : ""
            }`}
          >
            <DialogContent className="sm:max-w-md mx-auto my-16">
              <DialogHeader>
                <DialogTitle
                  style={{ color: playerColors[winner], fontSize: "2.5rem" }}
                >
                  Player {winner} won!
                </DialogTitle>
              </DialogHeader>
              <div className="flex flex-col space-y-4">
                <Button
                  variant="secondary"
                  onClick={() => (window.location.href = "/")}
                >
                  Play again
                </Button>
              </div>
              <DialogFooter className="sm:justify-start">
                <DialogClose asChild></DialogClose>
              </DialogFooter>
            </DialogContent>
          </div>
        </Dialog>

        <Dialog open={open} onOpenChange={setOpen}>
          <div className={`${open ? "backdrop-blur-sm fixed inset-0" : ""}`}>
            <DialogContent className="sm:max-w-md mx-auto my-16">
              <DialogHeader>
                <DialogTitle>Start Game</DialogTitle>
                <DialogDescription>
                  Choose how you want to play the game.
                </DialogDescription>
                <DialogDescription>
                  You can always send the link to your friends for them to
                  watch.
                </DialogDescription>
              </DialogHeader>
              <div className="flex flex-col space-y-4">
                <div className="flex items-center space-x-2">
                  <div className="grid flex-1 gap-2">
                    <Label htmlFor="link" className="sr-only">
                      Link
                    </Label>
                    <Input id="link" value={link} disabled={true} />
                  </div>
                  <Button
                    type="button"
                    size="sm"
                    className="px-3"
                    onClick={copyToClipboard}
                  >
                    <span className="sr-only">Copy</span>
                    <Copy className="h-4 w-4" />
                  </Button>
                </div>
                <Button variant="secondary" onClick={locally}>
                  Locally
                </Button>
                <Button variant="secondary" onClick={overInternet}>
                  Over Internet
                </Button>
              </div>
              <DialogFooter className="sm:justify-start">
                <DialogClose asChild></DialogClose>
              </DialogFooter>
            </DialogContent>
          </div>
        </Dialog>
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
