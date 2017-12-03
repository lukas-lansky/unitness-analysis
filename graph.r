png("outp.png", width=800, height=800)

colors <- rainbow(3)

codeanalcs <- read.csv("results/Microsoft.CodeAnalysis.CSharp.csv")
plot(codeanalcs[, 1], codeanalcs[, 2], type="n", xlab="Test coverage limit (sequence points)",
   ylab="Coverage (basis points of sequence points)", log="x")
lines(codeanalcs[, 1], codeanalcs[, 2], type="b", lwd=1.5, col = colors[2])

codeanal <- read.csv("results/Microsoft.CodeAnalysis.csv")
lines(codeanal[, 1], codeanal[, 2], type="b", lwd=1.5, col = colors[1])

codeanalvb <- read.csv("results/Microsoft.CodeAnalysis.VisualBasic.csv")
lines(codeanalvb[, 1], codeanalvb[, 2], type="b", lwd=1.5, col = colors[3])

# add a title and subtitle
title("Unit-ness analysis of Roslyn codebase")

legend(
    200, 10000,
    legend=c("Microsoft.CodeAnalysis", "Microsoft.CodeAnalysis.CSharp", "Microsoft.CodeAnalysis.VisualBasic"),
    col=colors, lty=1,  cex=1)

dev.off()