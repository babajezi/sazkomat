import * as React from "react"
import { Slot } from "@radix-ui/react-slot"

interface DialogContextValue {
  open: boolean
  onOpenChange: (open: boolean) => void
}

const DialogContext = React.createContext<DialogContextValue | undefined>(undefined)

function useDialog() {
  const context = React.useContext(DialogContext)
  if (!context) {
    throw new Error("Dialog components must be used within a Dialog")
  }
  return context
}

interface DialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  children: React.ReactNode
}

export function Dialog({ open, onOpenChange, children }: DialogProps) {
  return (
    <DialogContext.Provider value={{ open, onOpenChange }}>
      {children}
    </DialogContext.Provider>
  )
}

export function DialogContent({
  children,
  className = ""
}: {
  children: React.ReactNode
  className?: string
}) {
  const { open, onOpenChange } = useDialog()

  if (!open) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div
        className="fixed inset-0 bg-black/50"
        onClick={() => onOpenChange(false)}
      />
      <div className="relative z-50 bg-white rounded-lg shadow-lg max-w-lg w-full mx-4 max-h-[90vh] overflow-y-auto">
        <div className={`p-6 ${className}`}>{children}</div>
      </div>
    </div>
  )
}

export function DialogHeader({ children }: { children: React.ReactNode }) {
  return <div className="mb-4">{children}</div>
}

export function DialogTitle({
  children,
  className = ""
}: {
  children: React.ReactNode
  className?: string
}) {
  return <h2 className={`text-lg font-semibold ${className}`}>{children}</h2>
}

export function DialogDescription({ children }: { children: React.ReactNode }) {
  return <p className="text-sm text-gray-500 mt-1">{children}</p>
}

export function DialogFooter({ children }: { children: React.ReactNode }) {
  return <div className="flex justify-end gap-2 mt-6">{children}</div>
}

interface DialogTriggerProps {
  children: React.ReactNode
  asChild?: boolean
  className?: string
  [key: string]: any
}

export function DialogTrigger({
  children,
  asChild = false,
  className,
  ...props
}: DialogTriggerProps) {
  const { onOpenChange } = useDialog()
  const Comp = asChild ? Slot : "button"

  const handleClick = () => {
    onOpenChange(true)
  }

  return (
    <Comp
      onClick={handleClick}
      className={className}
      type={asChild ? undefined : "button"}
      {...props}
    >
      {children}
    </Comp>
  )
}
