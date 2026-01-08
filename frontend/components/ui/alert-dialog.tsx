import * as React from "react"
import { Button } from "./button"

interface AlertDialogContextValue {
  open: boolean
  onOpenChange: (open: boolean) => void
}

const AlertDialogContext = React.createContext<AlertDialogContextValue | undefined>(undefined)

function useAlertDialog() {
  const context = React.useContext(AlertDialogContext)
  if (!context) {
    throw new Error("AlertDialog components must be used within an AlertDialog")
  }
  return context
}

interface AlertDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  children: React.ReactNode
}

export function AlertDialog({ open, onOpenChange, children }: AlertDialogProps) {
  return (
    <AlertDialogContext.Provider value={{ open, onOpenChange }}>
      {children}
    </AlertDialogContext.Provider>
  )
}

export function AlertDialogContent({
  children,
  className = ""
}: {
  children: React.ReactNode
  className?: string
}) {
  const { open } = useAlertDialog()

  if (!open) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="fixed inset-0 bg-black/50" />
      <div className={`relative z-50 bg-white rounded-lg shadow-lg max-w-md w-full mx-4 p-6 ${className}`}>
        {children}
      </div>
    </div>
  )
}

export function AlertDialogHeader({ children }: { children: React.ReactNode }) {
  return <div className="mb-4">{children}</div>
}

export function AlertDialogTitle({
  children,
  className = ""
}: {
  children: React.ReactNode
  className?: string
}) {
  return <h2 className={`text-lg font-semibold ${className}`}>{children}</h2>
}

export function AlertDialogDescription({ children }: { children: React.ReactNode }) {
  return <p className="text-sm text-gray-500 mt-2">{children}</p>
}

export function AlertDialogFooter({ children }: { children: React.ReactNode }) {
  return <div className="flex justify-end gap-2 mt-6">{children}</div>
}

interface AlertDialogActionProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  children: React.ReactNode
  className?: string
}

export function AlertDialogAction({
  children,
  className = "",
  onClick,
  ...props
}: AlertDialogActionProps) {
  const { onOpenChange } = useAlertDialog()

  const handleClick = (e: React.MouseEvent<HTMLButtonElement>) => {
    onClick?.(e)
    onOpenChange(false)
  }

  return (
    <Button onClick={handleClick} className={className} {...props}>
      {children}
    </Button>
  )
}

export function AlertDialogCancel({
  children,
  className = ""
}: {
  children: React.ReactNode
  className?: string
}) {
  const { onOpenChange } = useAlertDialog()

  return (
    <Button variant="outline" onClick={() => onOpenChange(false)} className={className}>
      {children}
    </Button>
  )
}

interface AlertDialogTriggerProps {
  children: React.ReactNode
  className?: string
  [key: string]: any
}

export function AlertDialogTrigger({
  children,
  className,
  ...props
}: AlertDialogTriggerProps) {
  const { onOpenChange } = useAlertDialog()

  return (
    <button
      onClick={() => onOpenChange(true)}
      className={className}
      type="button"
      {...props}
    >
      {children}
    </button>
  )
}
