import * as React from "react"

export interface CheckboxProps
  extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string
}

const Checkbox = React.forwardRef<HTMLInputElement, CheckboxProps>(
  ({ className = "", label, ...props }, ref) => {
    return (
      <div className="flex items-center gap-2">
        <input
          type="checkbox"
          className={`h-4 w-4 rounded border-gray-300 text-gray-900 focus:ring-2 focus:ring-gray-950 ${className}`}
          ref={ref}
          {...props}
        />
        {label && (
          <label htmlFor={props.id} className="text-sm font-medium leading-none">
            {label}
          </label>
        )}
      </div>
    )
  }
)
Checkbox.displayName = "Checkbox"

export { Checkbox }
