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

export function GameStartDialog() {
  const generateLink = () => {
    const link =
      window.location.origin + "/" + Math.random().toString(36).substr(2, 6);
    return link;
  };

  const [open, setOpen] = React.useState(true);
  const [link, _] = React.useState(generateLink());

  const copyToClipboard = () => {
    navigator.clipboard.writeText(link);
  };
  const redirectToLink = () => {
    window.location.href = link;
  };
  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <div className={`${open ? "backdrop-blur-sm fixed inset-0" : ""}`}>
        <DialogContent className="sm:max-w-md mx-auto my-16">
          <DialogHeader>
            <DialogTitle>Start Game</DialogTitle>
            <DialogDescription>
              Choose how you want to play the game.
            </DialogDescription>
            <DialogDescription>
              You can always send the link to your friends for them to watch.
            </DialogDescription>
          </DialogHeader>
          <div className="flex flex-col space-y-4">
            {" "}
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
            <Button variant="secondary" onClick={() => setOpen(false)}>
              Locally
            </Button>
            <Button variant="secondary" onClick={generateLink}>
              Over Internet
            </Button>
          </div>
          <DialogFooter className="sm:justify-start">
            <DialogClose asChild></DialogClose>
          </DialogFooter>
        </DialogContent>
      </div>
    </Dialog>
  );
}
